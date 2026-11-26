using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// 030 · HU-CMP-01, 032 y 034: el comparador de un artículo. HTTP real contra PostgreSQL real: el cruce
/// por (proveedor_id, código) y los filtros de baja y vencido solo se prueban contra la base.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ComparadorApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ComparadorApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private sealed record Sembrado(int Articulo, int SinVinculos, int Norte, int Sur, int Baja);

    /// <summary>
    /// Un artículo vinculado a Norte (código del scrape en minúsculas y con espacios), a Sur (con URL y
    /// precio neto menor), a un proveedor de baja y a una fila vencida. Los Ids los genera la base.
    /// </summary>
    private static async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        var norte = new Proveedor { Codigo = "PRV-CN", RazonSocial = "Distribuidora Norte", Activo = true };
        var sur = new Proveedor { Codigo = "PRV-CS", RazonSocial = "Repuestos Sur", Activo = true, UrlReferencia = "https://sur.example/" };
        var baja = new Proveedor { Codigo = "PRV-CB", RazonSocial = "De baja", Activo = false, MotivoBaja = "Cerró", FechaBaja = DateTime.UtcNow };
        contexto.AddRange(norte, sur, baja);
        await contexto.SaveChangesAsync();
        var articulo = new Articulo { CodigoInterno = "CMP-1", Nombre = "Bomba de agua", Activo = true, UltimoProveedorId = norte.Id };
        var sinVinculos = new Articulo { CodigoInterno = "CMP-2", Nombre = "Correa", Activo = true };
        contexto.AddRange(articulo, sinVinculos);
        var corridas = new[] { norte, sur, baja }.ToDictionary(p => p.Id, p => new ScrapeCorrida
        {
            ProveedorId = p.Id, Estado = "SUCCESS", Inicio = DateTime.UtcNow, Fin = DateTime.UtcNow, CantidadItems = 0,
        });
        contexto.AddRange(corridas.Values);
        await contexto.SaveChangesAsync();

        ProveedorArticulo Fila(Proveedor p, string codigo, decimal? neto, decimal? lista, bool vencido = false) => new()
        {
            ProveedorId = p.Id, Codigo = codigo, Nombre = $"Bomba {codigo}", Marca = "SKF", PrecioNeto = neto, PrecioLista = lista,
            EstadoStock = "HAY", UrlImagen = $"https://img/{codigo.Trim()}.jpg",
            UltimaLectura = DateTime.UtcNow, Activo = true, Vencido = vencido, UltimaCorridaId = corridas[p.Id].Id,
        };
        contexto.AddRange(
            Fila(norte, " bn-1 ", 150m, 300m),
            Fila(sur, "BS-1", 120m, 260m),
            Fila(sur, "BS-VENCIDA", 10m, 20m, vencido: true),
            Fila(baja, "BB-1", 5m, 10m),
            Fila(sur, "BS-AJENA", 1m, 2m));   // del mismo proveedor, sin vincular: no es del artículo
        contexto.AddRange(
            new ArticuloCodigoAlternativo { ArticuloId = articulo.Id, ProveedorId = norte.Id, Codigo = "BN-1" },
            new ArticuloCodigoAlternativo { ArticuloId = articulo.Id, ProveedorId = sur.Id, Codigo = "BS-1" },
            new ArticuloCodigoAlternativo { ArticuloId = articulo.Id, ProveedorId = baja.Id, Codigo = "BB-1" });
        // La vencida está vinculada a otro artículo: el de Sur ya tiene su código (la D2 vigente hasta el 16).
        var otro = new Articulo { CodigoInterno = "CMP-3", Nombre = "Otra", Activo = true };
        contexto.Add(otro);
        await contexto.SaveChangesAsync();
        contexto.Add(new ArticuloCodigoAlternativo { ArticuloId = otro.Id, ProveedorId = sur.Id, Codigo = "BS-VENCIDA" });
        await contexto.SaveChangesAsync();
        return new(articulo.Id, sinVinculos.Id, norte.Id, sur.Id, baja.Id);
    }

    private static async Task<JsonElement> OkAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [PostgreSqlFact]
    public async Task CA001_030_CadaProveedorConSuCodigoYSuPrecio_OrdenadosPorNeto_SinBajasNiVencidos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture);

        var cuerpo = await OkAsync(await http.GetAsync($"/api/articulos/{s.Articulo}/comparador"));

        var filas = cuerpo.GetProperty("filas").EnumerateArray().Select(f => (
            Proveedor: f.GetProperty("proveedorId").GetInt32(), Codigo: f.GetProperty("codigo").GetString(),
            Neto: f.GetProperty("precioNeto").GetDecimal(), Lista: f.GetProperty("precioLista").GetDecimal(),
            Diferencia: f.GetProperty("diferenciaConMasBarato").GetDecimal(),
            Ultimo: f.GetProperty("ultimoComprado").GetBoolean())).ToList();
        // CA-005: el de baja no aparece; D-S7-10: la vencida tampoco; la ajena no está vinculada.
        Assert.Equal([(s.Sur, "BS-1", 120m, 260m, 0m, false), (s.Norte, " bn-1 ", 150m, 300m, 30m, true)], filas);
        Assert.Equal(JsonValueKind.Null, cuerpo.GetProperty("mensaje").ValueKind);
    }

    [PostgreSqlFact]
    public async Task CA001_034_LaUrlDeReferenciaVieneSoloSiElProveedorLaTiene()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture);

        var cuerpo = await OkAsync(await http.GetAsync($"/api/articulos/{s.Articulo}/comparador"));

        var urls = cuerpo.GetProperty("filas").EnumerateArray()
            .ToDictionary(f => f.GetProperty("proveedorId").GetInt32(), f => f.GetProperty("urlReferencia"));
        Assert.Equal("https://sur.example/", urls[s.Sur].GetString());
        Assert.Equal(JsonValueKind.Null, urls[s.Norte].ValueKind);
    }

    [PostgreSqlFact]
    public async Task CA004_030_SinFilasVinculadas_InformaElMensaje_YArticuloInexistenteEs404()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture);

        var cuerpo = await OkAsync(await http.GetAsync($"/api/articulos/{s.SinVinculos}/comparador"));

        Assert.Equal(0, cuerpo.GetProperty("filas").GetArrayLength());
        Assert.Equal("El artículo no tiene filas de catálogo vinculadas de ningún proveedor activo.", cuerpo.GetProperty("mensaje").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/articulos/999999/comparador")).StatusCode);
    }

    private HttpClient Cliente(PostgreSqlPruebas fixture)
    {
        var configuracion = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = "LaCentralTests", ["Jwt:Audience"] = "LaCentralTestsAudience",
        };
        var cliente = fixture.Liberar(_factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuracion));
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<LaCentralDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<LaCentralDbContext>>();
                servicios.AddDbContext<LaCentralDbContext>(o => o.UseNpgsql(fixture.ConexionAislada));
            });
        })).CreateClient();
        var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(configuracion).Build())
            .GenerarToken(1, "test.comparador", "OPERADOR", 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

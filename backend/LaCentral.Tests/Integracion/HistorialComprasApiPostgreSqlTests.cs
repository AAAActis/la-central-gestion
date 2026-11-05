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
/// 016 · HU-ART-06: el historial de compras de un artículo, del más reciente al más antiguo
/// (CA-002), con proveedor, costo unitario, fecha y factura. Solo lectura (CA-005).
/// HTTP real contra PostgreSQL real.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class HistorialComprasApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public HistorialComprasApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private static DateTime Dia(int mes, int dia) => new(2026, mes, dia, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tres registros del artículo, insertados en un orden que NO es el de las fechas (el más
    /// viejo entra último, con el Id más alto): ordenar por Id daría otro resultado. Más uno de
    /// otro artículo, que no tiene que aparecer.
    /// </summary>
    private static async Task<(int Articulo, int SinHistorial)> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuarioId = await fixture.CrearUsuarioAsync();
        await using var contexto = fixture.CrearContexto();
        var norte = new Proveedor { Codigo = "PRV-N", RazonSocial = "Distribuidora Norte", Activo = true };
        var sur = new Proveedor { Codigo = "PRV-S", RazonSocial = "Repuestos Sur", Activo = true };
        var articulo = new Articulo { CodigoInterno = "HIS-1", Nombre = "Filtro de aceite", Activo = true };
        var otro = new Articulo { CodigoInterno = "HIS-2", Nombre = "Correa", Activo = true };
        var sinHistorial = new Articulo { CodigoInterno = "HIS-3", Nombre = "Bujía", Activo = true };
        contexto.AddRange(norte, sur, articulo, otro, sinHistorial);
        await contexto.SaveChangesAsync();

        FacturaCompra Factura(Proveedor p, string numero, DateTime fecha) => new()
        {
            ProveedorId = p.Id, Numero = numero, FechaEmision = DateOnly.FromDateTime(fecha),
            Estado = "ACTIVA", Origen = "MANUAL", UsuarioId = usuarioId, FechaRegistro = fecha,
        };
        var f1 = Factura(norte, "A-0001-00000010", Dia(10, 5));
        var f2 = Factura(sur, "A-0002-00000020", Dia(10, 20));
        var f3 = Factura(norte, "A-0001-00000003", Dia(9, 1));
        contexto.AddRange(f1, f2, f3);
        await contexto.SaveChangesAsync();

        foreach (var (a, p, f, costo) in new[] { (articulo, norte, f1, 100m), (articulo, sur, f2, 120.5m), (otro, sur, f2, 7m), (articulo, norte, f3, 90m) })
        {
            contexto.ArticuloHistorialCompra.Add(new ArticuloHistorialCompra
            {
                ArticuloId = a.Id, ProveedorId = p.Id, FacturaCompraId = f.Id, PrecioCosto = costo, Fecha = f.FechaRegistro,
            });
            await contexto.SaveChangesAsync();
        }
        return (articulo.Id, sinHistorial.Id);
    }

    [PostgreSqlFact]
    public async Task Historial_DelMasRecienteAlMasAntiguo_ConProveedorCostoFechaYFactura()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (articulo, _) = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var respuesta = await http.GetAsync($"/api/articulos/{articulo}/historial-compras");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var filas = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().Select(f => (
            Fecha: f.GetProperty("fecha").GetDateTime().ToUniversalTime(),
            Proveedor: f.GetProperty("proveedor").GetString()!,
            Costo: f.GetProperty("precioCosto").GetDecimal(),
            Factura: f.GetProperty("numeroFactura").GetString()!,
            Anulado: f.GetProperty("anulado").GetBoolean())).ToList();
        Assert.Equal(new[]
        {
            (Dia(10, 20), "Repuestos Sur", 120.5m, "A-0002-00000020", false),
            (Dia(10, 5), "Distribuidora Norte", 100m, "A-0001-00000010", false),
            (Dia(9, 1), "Distribuidora Norte", 90m, "A-0001-00000003", false),
        }, filas.Select(f => (f.Fecha, f.Proveedor, f.Costo, f.Factura, f.Anulado)));
    }

    [PostgreSqlFact]
    public async Task Historial_DeUnArticuloSinCompras_EsUnaListaVacia()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (_, sinHistorial) = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var respuesta = await http.GetAsync($"/api/articulos/{sinHistorial}/historial-compras");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(0, (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
    }

    [PostgreSqlFact]
    public async Task Historial_DeUnArticuloInexistente_Es404ConMensaje()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR");

        var respuesta = await http.GetAsync("/api/articulos/999999/historial-compras");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("El artículo indicado no existe.", await respuesta.Content.ReadAsStringAsync());
    }

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol)
    {
        var cliente = fixture.Liberar(_factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(ConfiguracionJwt));
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<LaCentralDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<LaCentralDbContext>>();
                servicios.AddDbContext<LaCentralDbContext>(o => o.UseNpgsql(fixture.ConexionAislada));
            });
        })).CreateClient();

        var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(ConfiguracionJwt).Build())
            .GenerarToken(1, "test.historial", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

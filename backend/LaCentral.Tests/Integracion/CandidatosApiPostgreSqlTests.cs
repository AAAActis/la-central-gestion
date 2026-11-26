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
/// 053 · HU-CMP-06: candidatos de vinculación. HTTP real contra PostgreSQL real: la normalización
/// en SQL y la similitud de pg_trgm solo se prueban contra la base.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class CandidatosApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public CandidatosApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private sealed record Sembrado(int Norte, int Baja, int Articulo, Dictionary<string, int> Filas);

    /// <summary>
    /// Proveedor activo con cinco filas, una vencida, una vinculada a un artículo propio; y un
    /// proveedor dado de baja con una fila que coincide exacta y aproximada. Los Ids los genera la base.
    /// </summary>
    private static async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        var norte = new Proveedor { Codigo = "PRV-053", RazonSocial = "Distribuidora Norte", Activo = true };
        var baja = new Proveedor
        {
            Codigo = "PRV-053-B", RazonSocial = "Proveedor de baja", Activo = false,
            MotivoBaja = "Cerró", FechaBaja = DateTime.UtcNow,
        };
        var articulo = new Articulo { CodigoInterno = "ART-053", Nombre = "Termostato", Activo = true };
        contexto.AddRange(norte, baja, articulo);
        await contexto.SaveChangesAsync();

        var corridas = new Dictionary<int, ScrapeCorrida>();
        foreach (var p in new[] { norte, baja })
        {
            var corrida = new ScrapeCorrida
            {
                ProveedorId = p.Id, Estado = "SUCCESS", Inicio = DateTime.UtcNow, Fin = DateTime.UtcNow, CantidadItems = 0,
            };
            contexto.Add(corrida);
            corridas[p.Id] = corrida;
        }
        await contexto.SaveChangesAsync();

        ProveedorArticulo Fila(Proveedor p, string codigo, string nombre, string? numeroOriginal = null, bool vencido = false) => new()
        {
            ProveedorId = p.Id, Codigo = codigo, Nombre = nombre, NumeroOriginal = numeroOriginal,
            UltimaLectura = DateTime.UtcNow, Activo = true, Vencido = vencido, UltimaCorridaId = corridas[p.Id].Id,
        };
        var filas = new Dictionary<string, ProveedorArticulo>
        {
            ["exacta"] = Fila(norte, "GN/100", "Junta de tapa de cilindros"),
            ["parecida"] = Fila(norte, "BA-1", "Bomba de agua Fiat Palio 1.4"),
            ["menos"] = Fila(norte, "BA-2", "Bomba de agua Fiat"),
            ["ajena"] = Fila(norte, "FA-9", "Filtro de aceite"),
            ["vencida"] = Fila(norte, "BA-V", "Bomba de agua Fiat Palio 1.4", vencido: true),
            ["vinculada"] = Fila(norte, "tm-1 ", "Termostato Renault Clio", numeroOriginal: "7700 111"),
            ["prima"] = Fila(norte, "TV-5", "Valvula termostatica Renault Clio"),
            ["de baja"] = Fila(baja, "GN-100", "Bomba de agua Fiat Palio 1.4"),
        };
        contexto.AddRange(filas.Values);
        // La ficha guarda el código sin espacios en los extremos y en mayúsculas (D-S7-14).
        contexto.Add(new ArticuloCodigoAlternativo { ArticuloId = articulo.Id, ProveedorId = norte.Id, Codigo = "TM-1" });
        await contexto.SaveChangesAsync();
        return new(norte.Id, baja.Id, articulo.Id, filas.ToDictionary(f => f.Key, f => f.Value.Id));
    }

    private static List<JsonElement> Lista(JsonElement cuerpo, string nombre) => cuerpo.GetProperty(nombre).EnumerateArray().ToList();
    private static int[] Ids(JsonElement cuerpo, string nombre) => Lista(cuerpo, nombre).Select(f => f.GetProperty("id").GetInt32()).ToArray();

    private static async Task<JsonElement> OkAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return await respuesta.Content.ReadFromJsonAsync<JsonElement>();
    }

    [PostgreSqlFact]
    public async Task CA1_PrimeroLasExactasYAparteLasAproximadasDeMayorAMenorParecido()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var cuerpo = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("bomba de agua fiat palio")));

        Assert.Empty(Ids(cuerpo, "exactas"));
        Assert.Equal([s.Filas["parecida"], s.Filas["menos"]], Ids(cuerpo, "aproximadas"));
        var parecidos = Lista(cuerpo, "aproximadas").Select(f => f.GetProperty("parecido").GetDouble()).ToArray();
        Assert.True(parecidos[0] > parecidos[1], $"Tienen que venir de mayor a menor parecido: {string.Join(", ", parecidos)}.");
        Assert.Equal("Distribuidora Norte", Lista(cuerpo, "aproximadas")[0].GetProperty("proveedor").GetString());
    }

    [PostgreSqlFact]
    public async Task CA2_LaQueDifiereSoloEnSeparadoresMayusculasOAcentos_EsExacta_YNoAproximada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var porCodigo = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("gn 100")));
        var porNombre = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("JUNTA DE TAPA DE CILÍNDROS")));
        var porNumeroOriginal = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=7700-111"));

        Assert.Equal([s.Filas["exacta"]], Ids(porCodigo, "exactas"));
        Assert.Equal([s.Filas["exacta"]], Ids(porNombre, "exactas"));
        Assert.DoesNotContain(s.Filas["exacta"], Ids(porNombre, "aproximadas"));
        Assert.Equal([s.Filas["vinculada"]], Ids(porNumeroOriginal, "exactas"));
    }

    [PostgreSqlFact]
    public async Task CA3_LaFilaDeUnProveedorDeBaja_NoApareceNiExactaNiAproximada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        // «GN-100» es exacta para «gn 100» y su nombre es aproximado a «bomba de agua fiat palio».
        var exacta = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("gn 100")));
        var aproximada = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("bomba de agua fiat palio")));

        Assert.DoesNotContain(s.Filas["de baja"], Ids(exacta, "exactas").Concat(Ids(exacta, "aproximadas")));
        Assert.DoesNotContain(s.Filas["de baja"], Ids(aproximada, "exactas").Concat(Ids(aproximada, "aproximadas")));
        Assert.DoesNotContain(s.Baja, Lista(aproximada, "aproximadas").Select(f => f.GetProperty("proveedorId").GetInt32()));
    }

    [PostgreSqlFact]
    public async Task D_S7_10_LaFilaVencida_NoApareceComoCandidata()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var aproximada = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("bomba de agua fiat palio")));
        var exacta = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=ba-v"));

        Assert.DoesNotContain(s.Filas["vencida"], Ids(aproximada, "aproximadas"));
        Assert.Empty(Ids(exacta, "exactas"));
    }

    [PostgreSqlFact]
    public async Task CA4_LaFilaYaVinculada_IndicaAQueArticulo_YLasDemasNo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var cuerpo = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("termostato renault clio")));

        var filas = Lista(cuerpo, "exactas").Concat(Lista(cuerpo, "aproximadas"))
            .ToDictionary(f => f.GetProperty("id").GetInt32(), f => f.GetProperty("vinculado"));
        var vinculado = filas[s.Filas["vinculada"]];
        Assert.Equal((s.Articulo, "ART-053", "Termostato"),
            (vinculado.GetProperty("id").GetInt32(), vinculado.GetProperty("codigoInterno").GetString(), vinculado.GetProperty("nombre").GetString()));
        Assert.Equal(JsonValueKind.Null, filas[s.Filas["prima"]].ValueKind);
    }

    [PostgreSqlFact]
    public async Task CA5_ConArticulo_ElParecidoSeCalculaTambienContraSusFilasVinculadas()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        // «Termostato» solo no alcanza el umbral contra «Valvula termostatica Renault Clio»: la trae «Termostato Renault Clio», la fila vinculada.
        var soloNombre = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=Termostato"));
        var conArticulo = await OkAsync(await http.GetAsync($"/api/catalogo/candidatos?articuloId={s.Articulo}"));

        Assert.DoesNotContain(s.Filas["prima"], Ids(soloNombre, "aproximadas"));
        Assert.Contains(s.Filas["prima"], Ids(conArticulo, "aproximadas"));
    }

    [PostgreSqlFact]
    public async Task CA6_SinCandidatos_InformaElTextoBuscado()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR");

        var cuerpo = await OkAsync(await http.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("amortiguador trasero")));

        Assert.Empty(Ids(cuerpo, "exactas"));
        Assert.Empty(Ids(cuerpo, "aproximadas"));
        Assert.Equal("No hay candidatos para «amortiguador trasero».", cuerpo.GetProperty("mensaje").GetString());
    }

    [PostgreSqlFact]
    public async Task D_S7_11_ElUmbralSaleDeLaConfiguracion()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var estricto = Cliente(fixture, "OPERADOR", umbral: "0.95");

        var cuerpo = await OkAsync(await estricto.GetAsync("/api/catalogo/candidatos?texto=" + Uri.EscapeDataString("bomba de agua fiat palio")));

        // Con 0,3 vienen las dos (CA1); con 0,95 ninguna de las dos se parece tanto.
        Assert.DoesNotContain(s.Filas["menos"], Ids(cuerpo, "aproximadas"));
    }

    [PostgreSqlFact]
    public async Task SinTextoNiArticulo_Es400_YConArticuloInexistente_Es404()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR");

        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/catalogo/candidatos?texto=%20")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/catalogo/candidatos?articuloId=999999")).StatusCode);
    }

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol, string? umbral = null)
    {
        var configuracion = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = "LaCentralTests", ["Jwt:Audience"] = "LaCentralTestsAudience",
        };
        if (umbral is not null) configuracion["Catalogo:UmbralParecido"] = umbral;
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
            .GenerarToken(1, "test.candidatos", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
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
/// #147 · búsqueda paginada de clientes y proveedores de punta a punta: HTTP real contra
/// PostgreSQL real. Sin resultados es 200 con la misma envoltura que una página llena (CA-003
/// v1.1 de HU-CLI-02 y HU-PRO-02); las páginas no se pisan entre sí (orden estable).
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class BusquedaPaginadaApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public BusquedaPaginadaApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    [PostgreSqlFact]
    public async Task Clientes_SinResultados200ConEnvolturaVacia_YPaginasSinSolaparse()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        foreach (var (codigo, nombre) in new[] { ("CLI-901", "Repuestos Paginados Uno"), ("CLI-902", "Repuestos Paginados Dos"), ("CLI-903", "Repuestos Paginados Tres") })
            await using (var contexto = fixture.CrearContexto())
                Assert.True((await new CrearClienteUseCase(new ClienteRepositorio(contexto), DoblesBaja.Contexto(usuario).Object)
                    .EjecutarAsync(new CrearClienteRequest(codigo, nombre, null, "CONSUMIDOR_FINAL", "Contado", null, null))).IsSuccess);
        using var http = Cliente(fixture);

        await AfirmarPaginacion(http, "/api/clientes", "Repuestos Paginados");
    }

    [PostgreSqlFact]
    public async Task Proveedores_SinResultados200ConEnvolturaVacia_YPaginasSinSolaparse()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        foreach (var (codigo, nombre) in new[] { ("PRV-901", "Repuestos Paginados Uno"), ("PRV-902", "Repuestos Paginados Dos"), ("PRV-903", "Repuestos Paginados Tres") })
            await using (var contexto = fixture.CrearContexto())
                Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                    .EjecutarAsync(new CrearProveedorRequest(codigo, nombre, null, null, null, null))).IsSuccess);
        using var http = Cliente(fixture);

        await AfirmarPaginacion(http, "/api/proveedores", "Repuestos Paginados");
    }

    // B1 del verificador (05/11/2026): con razones sociales distintas nunca hay empate de similitud,
    // y el desempate por Id no se podía ver. Acá empatan las tres, y el orden físico de la tabla
    // no es el de los Ids: un UPDATE deja la versión nueva de la fila al final (MVCC). Sin
    // ThenBy(Id), las páginas salen en el orden físico.
    [PostgreSqlFact]
    public Task Clientes_ConSimilitudEmpatada_LasPaginasSeOrdenanPorId() => AfirmarDesempatePorId("cliente", "/api/clientes");

    [PostgreSqlFact]
    public Task Proveedores_ConSimilitudEmpatada_LasPaginasSeOrdenanPorId() => AfirmarDesempatePorId("proveedor", "/api/proveedores");

    private async Task AfirmarDesempatePorId(string tabla, string ruta)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int[] ids;
        await using (var contexto = fixture.CrearContexto())
        {
            // La tabla es una de dos constantes de este archivo, no una entrada.
            string insertar = "INSERT INTO " + tabla + " (codigo, razon_social) VALUES ({0}, 'Repuestos Empatados')",
                porId = "SELECT id AS \"Value\" FROM " + tabla + " ORDER BY id",
                actualizar = "UPDATE " + tabla + " SET codigo = 'EMP-1b' WHERE id = {0}",
                fisicoSql = "SELECT id AS \"Value\" FROM " + tabla;
            foreach (var codigo in new[] { "EMP-1", "EMP-2", "EMP-3" })
                await contexto.Database.ExecuteSqlRawAsync(insertar, codigo);
            ids = await contexto.Database.SqlQueryRaw<int>(porId).ToArrayAsync();
            await contexto.Database.ExecuteSqlRawAsync(actualizar, ids[0]);
            var fisico = await contexto.Database.SqlQueryRaw<int>(fisicoSql).ToArrayAsync();
            Assert.NotEqual(ids, fisico); // precondición: el orden físico ya no es el de los Ids
        }
        using var http = Cliente(fixture);

        var paginas = new List<int>();
        foreach (var pagina in new[] { 1, 2 })
            paginas.AddRange((await http.GetFromJsonAsync<JsonElement>($"{ruta}?texto=Repuestos%20Empatados&pagina={pagina}&tamanoPagina=2"))
                .GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()));

        Assert.Equal(ids, paginas);
    }

    private static async Task AfirmarPaginacion(HttpClient http, string ruta, string texto)
    {
        var vacia = await http.GetAsync($"{ruta}?texto=zzqqxxnada");
        Assert.Equal(HttpStatusCode.OK, vacia.StatusCode);
        var cuerpoVacio = await vacia.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, cuerpoVacio.GetProperty("items").GetArrayLength());
        Assert.Equal(0, cuerpoVacio.GetProperty("total").GetInt32());
        Assert.Equal(1, cuerpoVacio.GetProperty("pagina").GetInt32());
        Assert.Equal(20, cuerpoVacio.GetProperty("tamanoPagina").GetInt32());

        var t = Uri.EscapeDataString(texto);
        var uno = await http.GetFromJsonAsync<JsonElement>($"{ruta}?texto={t}&pagina=1&tamanoPagina=2");
        var dos = await http.GetFromJsonAsync<JsonElement>($"{ruta}?texto={t}&pagina=2&tamanoPagina=2");
        var tres = await http.GetFromJsonAsync<JsonElement>($"{ruta}?texto={t}&pagina=3&tamanoPagina=2");

        Assert.Equal(3, uno.GetProperty("total").GetInt32());
        Assert.Equal(2, uno.GetProperty("items").GetArrayLength());
        Assert.Equal(3, dos.GetProperty("total").GetInt32());
        Assert.Equal(2, dos.GetProperty("pagina").GetInt32());
        Assert.Equal(1, dos.GetProperty("items").GetArrayLength());
        Assert.Equal(0, tres.GetProperty("items").GetArrayLength());
        Assert.Equal(3, tres.GetProperty("total").GetInt32());

        // Las dos páginas juntas son los tres, sin repetidos: el orden es estable y cada fila trae su Id.
        var ids = uno.GetProperty("items").EnumerateArray().Concat(dos.GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("id").GetInt32()).ToList();
        Assert.Equal(3, ids.Distinct().Count());
        Assert.All(ids, id => Assert.True(id > 0));
    }

    private HttpClient Cliente(PostgreSqlPruebas fixture)
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
            .GenerarToken(1, "test.paginacion", "OPERADOR", 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

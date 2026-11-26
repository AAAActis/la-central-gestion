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
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// 054a · HU-CMP-07: vincular una fila de catálogo. HTTP real contra PostgreSQL real; la fila se
/// relee con un contexto nuevo. Requiere el script 16 en el esquema fuente.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class VinculacionApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public VinculacionApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private sealed record Sembrado(int Usuario, int Proveedor, int Articulo, int Otro, int DeBaja, Dictionary<string, int> Filas);

    private static async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        var sucursal = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
        var usuario = new Usuario
        {
            NombreUsuario = "operador.vincula", HashContrasena = new ServicioHash().HashearClave("clave"),
            RolId = await contexto.Rol.Where(r => r.Nombre == "OPERADOR").Select(r => r.Id).SingleAsync(),
            SucursalId = sucursal, Activo = true,
        };
        var proveedor = new Proveedor { Codigo = "PRV-054", RazonSocial = "Distribuidora Sur", Activo = true };
        var articulo = new Articulo { CodigoInterno = "ART-054", Nombre = "Bomba de agua", Activo = true };
        var otro = new Articulo { CodigoInterno = "ART-054-B", Nombre = "Bomba de nafta", Activo = true };
        var deBaja = new Articulo
        {
            CodigoInterno = "ART-054-X", Nombre = "Discontinuado", Activo = false, MotivoBaja = "Sin uso", FechaBaja = DateTime.UtcNow,
        };
        contexto.AddRange(usuario, proveedor, articulo, otro, deBaja);
        await contexto.SaveChangesAsync();
        var corrida = new ScrapeCorrida
        {
            ProveedorId = proveedor.Id, Estado = "SUCCESS", Inicio = DateTime.UtcNow, Fin = DateTime.UtcNow, CantidadItems = 0,
        };
        contexto.Add(corrida);
        await contexto.SaveChangesAsync();

        ProveedorArticulo Fila(string codigo, string nombre, bool vencido = false) => new()
        {
            ProveedorId = proveedor.Id, Codigo = codigo, Nombre = nombre, UltimaLectura = DateTime.UtcNow,
            Activo = true, Vencido = vencido, UltimaCorridaId = corrida.Id,
        };
        var filas = new Dictionary<string, ProveedorArticulo>
        {
            ["marca a"] = Fila(" ba-7 ", "Bomba de agua marca A"),
            ["marca b"] = Fila("BA-8", "Bomba de agua marca B"),
            ["vencida"] = Fila("BA-9", "Bomba de agua vieja", vencido: true),
        };
        contexto.AddRange(filas.Values);
        await contexto.SaveChangesAsync();
        return new(usuario.Id, proveedor.Id, articulo.Id, otro.Id, deBaja.Id, filas.ToDictionary(f => f.Key, f => f.Value.Id));
    }

    private static Task<HttpResponseMessage> VincularAsync(HttpClient http, int fila, int articuloId)
        => http.PostAsJsonAsync($"/api/catalogo/filas/{fila}/vinculacion", new { articuloId });

    [PostgreSqlFact]
    public async Task CA1_CA10_ElOperadorVincula_YQuedaUnaFilaConElCodigoNormalizadoElUsuarioYLaFecha()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR", s.Usuario);
        var antes = DateTime.UtcNow.AddMinutes(-1);

        var respuesta = await VincularAsync(http, s.Filas["marca a"], s.Articulo);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        await using var nuevo = fixture.CrearContexto();
        var fila = await LeerAsync(nuevo, s.Proveedor, "BA-7");
        Assert.NotNull(fila);
        Assert.Equal(s.Articulo, fila.Value.ArticuloId);
        Assert.Equal(s.Usuario, fila.Value.UsuarioId);
        Assert.True(fila.Value.Fecha > antes, $"La fecha de vinculación tiene que ser la de ahora: {fila.Value.Fecha:O}.");
    }

    [PostgreSqlFact]
    public async Task CA2_UnaVezVinculada_LaBusquedaDeEseCodigoLaDaExactaConSuArticulo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR", s.Usuario);
        Assert.Equal(HttpStatusCode.OK, (await VincularAsync(http, s.Filas["marca a"], s.Articulo)).StatusCode);

        var cuerpo = await (await http.GetAsync("/api/catalogo/candidatos?texto=BA-7")).Content.ReadFromJsonAsync<JsonElement>();

        var exacta = Assert.Single(cuerpo.GetProperty("exactas").EnumerateArray());
        Assert.Equal(s.Filas["marca a"], exacta.GetProperty("id").GetInt32());
        Assert.Equal(s.Articulo, exacta.GetProperty("vinculado").GetProperty("id").GetInt32());
    }

    [PostgreSqlFact]
    public async Task CA3_DosCodigosDelMismoProveedorAlMismoArticulo_QuedanLosDos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR", s.Usuario);

        Assert.Equal(HttpStatusCode.OK, (await VincularAsync(http, s.Filas["marca a"], s.Articulo)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await VincularAsync(http, s.Filas["marca b"], s.Articulo)).StatusCode);

        await using var nuevo = fixture.CrearContexto();
        var codigos = await nuevo.ArticuloCodigoAlternativo.Where(c => c.ArticuloId == s.Articulo && c.ProveedorId == s.Proveedor)
            .OrderBy(c => c.Codigo).Select(c => c.Codigo).ToListAsync();
        Assert.Equal(["BA-7", "BA-8"], codigos);
    }

    [PostgreSqlFact]
    public async Task CA6_ElCodigoYaVinculadoAOtroArticulo_Es409ConCualEs_YNoCambiaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR", s.Usuario);
        Assert.Equal(HttpStatusCode.OK, (await VincularAsync(http, s.Filas["marca a"], s.Otro)).StatusCode);

        var respuesta = await VincularAsync(http, s.Filas["marca a"], s.Articulo);

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("ART-054-B", await respuesta.Content.ReadAsStringAsync());
        await using var nuevo = fixture.CrearContexto();
        Assert.Equal(s.Otro, (await LeerAsync(nuevo, s.Proveedor, "BA-7"))!.Value.ArticuloId);
    }

    [PostgreSqlFact]
    public async Task FilaVencida_ArticuloDeBaja_YSinArticulo_Son400_YNoEscribenNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var http = Cliente(fixture, "OPERADOR", s.Usuario);

        Assert.Equal(HttpStatusCode.BadRequest, (await VincularAsync(http, s.Filas["vencida"], s.Articulo)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await VincularAsync(http, s.Filas["marca a"], s.DeBaja)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await VincularAsync(http, s.Filas["marca a"], 0)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await VincularAsync(http, 999999, s.Articulo)).StatusCode);

        await using var nuevo = fixture.CrearContexto();
        Assert.False(await nuevo.ArticuloCodigoAlternativo.AnyAsync(c => c.ProveedorId == s.Proveedor));
    }

    /// <summary>usuario_id y fecha_vinculacion son del 16 y todavía no están en el modelo scaffoldeado.</summary>
    private static async Task<(int ArticuloId, int? UsuarioId, DateTime? Fecha)?> LeerAsync(LaCentralDbContext contexto, int proveedor, string codigo)
    {
        var conexion = (NpgsqlConnection)contexto.Database.GetDbConnection();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT articulo_id, usuario_id, fecha_vinculacion FROM articulo_codigo_alternativo
             WHERE proveedor_id = @proveedor AND codigo = @codigo
            """, conexion);
        comando.Parameters.AddWithValue("proveedor", proveedor);
        comando.Parameters.AddWithValue("codigo", codigo);
        await using var lector = await comando.ExecuteReaderAsync();
        if (!await lector.ReadAsync()) return null;
        return (lector.GetInt32(0), lector.IsDBNull(1) ? null : lector.GetInt32(1), lector.IsDBNull(2) ? null : lector.GetDateTime(2));
    }

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol, int usuarioId)
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
            .GenerarToken(usuarioId, "operador.vincula", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

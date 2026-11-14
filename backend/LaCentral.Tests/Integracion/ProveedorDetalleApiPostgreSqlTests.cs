using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
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
/// 045 · CA-002 y CA-003: la ficha del proveedor trae todos sus teléfonos y direcciones y su URL
/// de referencia. HTTP real contra PostgreSQL real.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ProveedorDetalleApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ProveedorDetalleApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    [PostgreSqlFact]
    public async Task Detalle_TraeTodosLosTelefonosDireccionesYLaUrl()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto)).EjecutarAsync(
                new CrearProveedorRequest("PRV-045", "Proveedor con ficha", "30-12345678-9", "https://proveedor.example/catalogo",
                    new List<string> { "351 111-1111", "351 222-2222" }, new List<string> { "Av. Colón 100", "Bv. San Juan 200" }))).IsSuccess);
        int id;
        await using (var contexto = fixture.CrearContexto())
            id = (await contexto.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "PRV-045")).Id;
        using var http = Cliente(fixture, "OPERADOR");

        var respuesta = await http.GetAsync($"/api/proveedores/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ficha = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, ficha.GetProperty("id").GetInt32());
        Assert.Equal("Proveedor con ficha", ficha.GetProperty("razonSocial").GetString());
        Assert.Equal("https://proveedor.example/catalogo", ficha.GetProperty("urlReferencia").GetString());
        Assert.Equal(new[] { "351 111-1111", "351 222-2222" },
            ficha.GetProperty("telefonos").EnumerateArray().Select(t => t.GetString()).Order());
        Assert.Equal(new[] { "Av. Colón 100", "Bv. San Juan 200" },
            ficha.GetProperty("direcciones").EnumerateArray().Select(d => d.GetString()).Order());
    }

    // Verificador-tdd, 05/11/2026: sin esto sobrevivía devolver siempre Activo = true y MotivoBaja = null.
    // La pantalla elige entre Modificar y Reactivar por `activo` y muestra el motivo.
    [PostgreSqlFact]
    public async Task Detalle_DeUnProveedorDadoDeBaja_TraeActivoFalsoYElMotivo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto)).EjecutarAsync(
                new CrearProveedorRequest("PRV-BAJ", "Proveedor que cerró", null, null, null, null))).IsSuccess);
        int id;
        await using (var contexto = fixture.CrearContexto())
            id = (await contexto.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "PRV-BAJ")).Id;
        using var http = Cliente(fixture, "OPERADOR", await fixture.CrearUsuarioAsync());

        var activo = await http.GetFromJsonAsync<JsonElement>($"/api/proveedores/{id}");
        Assert.True(activo.GetProperty("activo").GetBoolean());
        Assert.Equal(JsonValueKind.Null, activo.GetProperty("motivoBaja").ValueKind);

        var baja = await http.PostAsJsonAsync($"/api/proveedores/{id}/baja", new { confirmacion = "PRV-BAJ", motivoBaja = "Dejó de operar" });
        Assert.True(baja.IsSuccessStatusCode, await baja.Content.ReadAsStringAsync());

        var dadoDeBaja = await http.GetFromJsonAsync<JsonElement>($"/api/proveedores/{id}");
        Assert.False(dadoDeBaja.GetProperty("activo").GetBoolean());
        Assert.Equal("Dejó de operar", dadoDeBaja.GetProperty("motivoBaja").GetString());
    }

    // #168 (deuda del #166): un PUT sin el tipo de comprobante habitual no lo borra en silencio.
    [PostgreSqlFact]
    public async Task Modificar_SinTipoHabitualEnElCuerpo_Es400YLaFilaLoConserva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var id = await CrearConTipoAsync(fixture, "PRV-168");
        using var http = Cliente(fixture, "OPERADOR", await fixture.CrearUsuarioAsync());

        var respuesta = await http.PutAsJsonAsync($"/api/proveedores/{id}",
            new { razonSocial = "Otro nombre", cuit = "30-12345678-9", telefonos = new string[0], direcciones = new string[0] });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("tipo de comprobante habitual", await respuesta.Content.ReadAsStringAsync());
        await using var relectura = fixture.CrearContexto();
        var fila = await relectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == id);
        Assert.Equal('B', fila.TipoComprobanteHabitual);
        Assert.Equal("Proveedor B", fila.RazonSocial);
    }

    // #168: mandarlo en null sigue siendo la forma de dejarlo vacío (así lo manda la pantalla).
    [PostgreSqlFact]
    public async Task Modificar_ConTipoHabitualNull_LoVacia()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var id = await CrearConTipoAsync(fixture, "PRV-169");
        using var http = Cliente(fixture, "OPERADOR", await fixture.CrearUsuarioAsync());

        var respuesta = await http.PutAsJsonAsync($"/api/proveedores/{id}",
            new { razonSocial = "Proveedor B", cuit = "30-12345678-9", telefonos = new string[0], direcciones = new string[0], tipoComprobanteHabitual = (string?)null });

        Assert.True(respuesta.IsSuccessStatusCode, await respuesta.Content.ReadAsStringAsync());
        await using var relectura = fixture.CrearContexto();
        Assert.Null((await relectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == id)).TipoComprobanteHabitual);
    }

    private static async Task<int> CrearConTipoAsync(PostgreSqlPruebas fixture, string codigo)
    {
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto)).EjecutarAsync(
                new CrearProveedorRequest(codigo, "Proveedor B", "30-12345678-9", null, null, null, "B"))).IsSuccess);
        await using var lectura = fixture.CrearContexto();
        return (await lectura.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == codigo)).Id;
    }

    // No bloqueante del verificador: el endpoint nuevo exige sesión (hereda el [Authorize] de la clase).
    [PostgreSqlFact]
    public async Task Detalle_SinSesion_Es401()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR", 1);
        http.DefaultRequestHeaders.Authorization = null;

        Assert.Equal(HttpStatusCode.Unauthorized, (await http.GetAsync("/api/proveedores/1")).StatusCode);
    }

    [PostgreSqlFact]
    public async Task Detalle_DeUnIdInexistente_Es404ConMensaje()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR");

        var respuesta = await http.GetAsync("/api/proveedores/999999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Contains("El proveedor indicado no existe.", await respuesta.Content.ReadAsStringAsync());
    }

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol, int usuarioId = 1)
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
            .GenerarToken(usuarioId, "test.proveedores", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

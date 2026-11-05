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
            .GenerarToken(1, "test.proveedores", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

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
/// Deuda del #151: la ficha de un cliente dado de baja trae el motivo, igual que la de proveedor.
/// Es el de la ÚLTIMA baja (D-S5-08): con dos bajas, el de la segunda. HTTP real contra PostgreSQL
/// real; cada lectura es un pedido nuevo, con su propio contexto.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ClienteDetalleApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ClienteDetalleApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    [PostgreSqlFact]
    public async Task Detalle_DeUnClienteDadoDeBajaDosVeces_TraeActivoFalsoYElMotivoDeLaUltima()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR", await fixture.CrearUsuarioAsync());
        var alta = await http.PostAsJsonAsync("/api/clientes", new
        {
            codigo = "CLI-151", razonSocial = "Cliente que cerró", cuit = (string?)null, condicionFiscal = (string?)null,
            condicionPago = "Contado", telefonos = Array.Empty<string>(), direcciones = Array.Empty<string>(),
        });
        Assert.True(alta.IsSuccessStatusCode, await alta.Content.ReadAsStringAsync());
        int id;
        await using (var lectura = fixture.CrearContexto())
            id = (await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Codigo == "CLI-151")).Id;

        var activo = await http.GetFromJsonAsync<JsonElement>($"/api/clientes/{id}");
        Assert.True(activo.GetProperty("activo").GetBoolean());
        Assert.True(activo.TryGetProperty("motivoBaja", out var sinMotivo), "La ficha no trae motivoBaja.");
        Assert.Equal(JsonValueKind.Null, sinMotivo.ValueKind);

        await DarDeBaja(http, id, "Primera baja");
        var reactivacion = await http.PostAsync($"/api/clientes/{id}/reactivacion", null);
        Assert.True(reactivacion.IsSuccessStatusCode, await reactivacion.Content.ReadAsStringAsync());

        // Verificador-tdd, 08/11/2026: sin esto sobrevivía `Activo ? null : MotivoBaja`. Reactivado,
        // la ficha conserva el motivo de su última baja como rastro (D10, HU-CLI-05 CA-003).
        var reactivado = await http.GetFromJsonAsync<JsonElement>($"/api/clientes/{id}");
        Assert.True(reactivado.GetProperty("activo").GetBoolean());
        Assert.Equal("Primera baja", reactivado.GetProperty("motivoBaja").GetString());

        await DarDeBaja(http, id, "Segunda baja");

        var ficha = await http.GetFromJsonAsync<JsonElement>($"/api/clientes/{id}");
        Assert.False(ficha.GetProperty("activo").GetBoolean());
        Assert.True(ficha.TryGetProperty("motivoBaja", out var motivo), "La ficha no trae motivoBaja.");
        Assert.Equal("Segunda baja", motivo.GetString());
        await using (var lectura = fixture.CrearContexto())
            Assert.Equal(2, await lectura.ClienteHistorialBaja.CountAsync(h => h.ClienteId == id));
    }

    private static async Task DarDeBaja(HttpClient http, int id, string motivo)
    {
        // Sin CUIT, la confirmación es el código (DarDeBajaClienteUseCase).
        var baja = await http.PostAsJsonAsync($"/api/clientes/{id}/baja", new { confirmacion = "CLI-151", motivo });
        Assert.True(baja.IsSuccessStatusCode, await baja.Content.ReadAsStringAsync());
    }

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol, int usuarioId)
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
            .GenerarToken(usuarioId, "test.clientes", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }
}

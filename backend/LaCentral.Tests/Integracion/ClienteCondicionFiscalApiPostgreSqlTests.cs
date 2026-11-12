using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
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
/// 045 · la condición fiscal vacía es un estado válido y se puede guardar vacía (decisión de Santi,
/// 05/11/2026): los clientes importados de Multisoft vienen sin informarla. La columna es nullable
/// y el CHECK cliente_condicion_fiscal_valida admite NULL. Se relee la fila con un contexto nuevo.
/// </summary>
[Trait("Api", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ClienteCondicionFiscalApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ClienteCondicionFiscalApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    [PostgreSqlFact]
    public Task AltaYModificacion_ConCondicionFiscalVacia_GuardanNull() => AfirmarGuardaNull("");

    // Verificador-tdd, 05/11/2026: el frontend manda null, no "". Sin esto sobrevivía `?? "MONOTRIBUTO"`
    // en ClientesController (alta) y en ModificarClienteUseCase.
    [PostgreSqlFact]
    public Task AltaYModificacion_ConCondicionFiscalNull_GuardanNull() => AfirmarGuardaNull(null);

    private async Task AfirmarGuardaNull(string? vacia)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var http = Cliente(fixture, "OPERADOR", await fixture.CrearUsuarioAsync());

        var alta = await http.PostAsJsonAsync("/api/clientes", new
        {
            codigo = "CLI-045", razonSocial = "Cliente sin condición", cuit = (string?)null,
            condicionFiscal = vacia, condicionPago = "Contado", telefonos = new[] { "351 000-0000" }, direcciones = Array.Empty<string>(),
        });
        Assert.Equal(HttpStatusCode.OK, alta.StatusCode);
        int id;
        await using (var lectura = fixture.CrearContexto())
        {
            var fila = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Codigo == "CLI-045");
            Assert.Null(fila.CondicionFiscal);
            id = fila.Id;
        }

        // Se informa y después se vuelve a dejar vacía: la modificación también la guarda vacía.
        foreach (var (condicion, esperado) in new[] { ((string?)"MONOTRIBUTO", (string?)"MONOTRIBUTO"), (vacia, null) })
        {
            var modificacion = await http.PutAsJsonAsync($"/api/clientes/{id}", new
            {
                codigo = "CLI-045", razonSocial = "Cliente sin condición", cuit = (string?)null,
                condicionFiscal = condicion, condicionPago = "Contado", telefonos = new[] { "351 000-0000" }, direcciones = Array.Empty<string>(),
            });
            Assert.Equal(HttpStatusCode.NoContent, modificacion.StatusCode);
            await using var lectura = fixture.CrearContexto();
            Assert.Equal(esperado, (await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == id)).CondicionFiscal);
        }
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

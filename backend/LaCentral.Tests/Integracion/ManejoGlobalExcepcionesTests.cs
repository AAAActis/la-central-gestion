using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.UseCases.Puertos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// ALTO 6 · Manejador global de excepciones. Corre en el entorno por defecto de
/// WebApplicationFactory (Development), que es justamente donde la página de
/// excepciones del desarrollador expondría el stack trace si nada la intercepta.
/// </summary>
public class ManejoGlobalExcepcionesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";
    private const string SecretoInterno = "detalle-interno-que-no-debe-salir";

    private readonly WebApplicationFactory<Program> _factory;

    public ManejoGlobalExcepcionesTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task ExcepcionNoManejada_Devuelve500ConProblemDetailsSinDatosInternos()
    {
        var respuesta = await BuscarClientesLanzando(new InvalidOperationException(SecretoInterno));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SecretoInterno, cuerpo);
        Assert.DoesNotContain(nameof(InvalidOperationException), cuerpo);
        Assert.DoesNotContain("   at ", cuerpo);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task ViolacionDeUnicidadReconocida_Devuelve409ConProblemDetails()
    {
        var respuesta = await BuscarClientesLanzando(ViolacionUnicidad("cliente_codigo_key"));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain(SecretoInterno, cuerpo);
        Assert.DoesNotContain("cliente_codigo_key", cuerpo);
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(409, json.RootElement.GetProperty("status").GetInt32());
        Assert.Contains("código", json.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ViolacionDeUnicidadNoReconocida_NoSeConvierteEnConflicto()
    {
        var respuesta = await BuscarClientesLanzando(ViolacionUnicidad("indice_que_nadie_registro"));

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("indice_que_nadie_registro", cuerpo);
        Assert.DoesNotContain(SecretoInterno, cuerpo);
    }

    // Misma forma que produce EF Core contra PostgreSQL: DbUpdateException con
    // la PostgresException del motor como causa.
    internal static DbUpdateException ViolacionUnicidad(string restriccion) => new(
        SecretoInterno,
        new PostgresException(SecretoInterno, "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation,
            constraintName: restriccion));

    private async Task<HttpResponseMessage> BuscarClientesLanzando(Exception excepcion)
    {
        var repo = new Mock<IClienteRepositorio>();
        repo.Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(excepcion);

        var cliente = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtKey,
                    ["Jwt:Issuer"] = JwtIssuer,
                    ["Jwt:Audience"] = JwtAudience
                }));
            builder.ConfigureTestServices(s => s.AddScoped(_ => repo.Object));
        }).CreateClient();

        var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience
            }).Build()).GenerarToken(1, "test.excepciones", "Operador", 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await cliente.GetAsync("/api/clientes?texto=abc");
    }
}

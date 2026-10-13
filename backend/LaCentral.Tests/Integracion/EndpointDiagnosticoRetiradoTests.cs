using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace LaCentral.Tests.Integracion;

public class EndpointDiagnosticoRetiradoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointDiagnosticoRetiradoTests(WebApplicationFactory<Program> factory)
        => _factory = factory;

    [Fact]
    public async Task TestDbRetirado_Devuelve404SinAccederALaBase()
    {
        using var aplicacion = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "ClaveExclusivaDePruebasDeAlMenos32Caracteres",
                    ["Jwt:Issuer"] = "LaCentralTests",
                    ["Jwt:Audience"] = "LaCentralTests"
                })));
        using var http = aplicacion.CreateClient();
        using var respuesta = await http.GetAsync("/api/test-db");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}

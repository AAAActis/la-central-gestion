using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using Moq;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Acceso;

public class LoginIntegracionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoginIntegracionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_RutaCorrectaYCredencialesValidas_DevuelveTokenYStatus200()
    {
        // Arrange: Preparamos los mocks
        var repoMock = new Mock<IUsuarioRepositorio>();
        var hashMock = new Mock<IServicioHash>();

        var usuarioMock = new Usuario { Id = 1, NombreUsuario = "admin", HashContrasena = "hash_real", RolId = 2, Activo = true };
        
        repoMock.Setup(r => r.ObtenerPorNombreAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioMock);
            
        hashMock.Setup(h => h.VerificarClave("clave_buena", "hash_real"))
            .Returns(true);

        // Interceptamos la API para inyectar los mocks en vez de las clases reales
        var cliente = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => repoMock.Object);
                services.AddScoped(_ => hashMock.Object);
            });
        }).CreateClient();

        // El objeto tiene que coincidir con tu LoginRequest de la API
        var request = new { NombreUsuario = "admin", Contrasena = "clave_buena" };

        // Act: Le pegamos directo al endpoint HTTP como si fuéramos Postman
        var respuesta = await cliente.PostAsJsonAsync("/api/acceso/login", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        
        var contenido = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("token", contenido.ToLower());
    }
}
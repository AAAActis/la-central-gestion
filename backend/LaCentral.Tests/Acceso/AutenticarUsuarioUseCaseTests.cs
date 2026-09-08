using Moq;
using Xunit;
using LaCentral.UseCases; 
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Models; 
using LaCentral.UseCases.Acceso.Dtos; // Para SesionDto

namespace LaCentral.Tests.Acceso;

public class AutenticarUsuarioUseCaseTests
{
    // Los mensajes exactos que pusiste en tu caso de uso
    private const string MensajeCredencialesInvalidas = "Usuario o contraseña incorrectos.";
    private const string MensajeUsuarioInactivo = "El usuario no tiene acceso al sistema.";

    private readonly Mock<IUsuarioRepositorio> _repoMock = new();
    private readonly Mock<IServicioHash> _hashMock = new();
    private readonly Mock<IGeneradorToken> _tokenMock = new();

    private AutenticarUsuarioUseCase CrearCasoDeUso() => 
        new AutenticarUsuarioUseCase(_repoMock.Object, _hashMock.Object, _tokenMock.Object);

    [Fact]
    public async Task Ejecutar_UsuarioInexistente_DevuelveFalloGenerico()
    {
        // Arrange
        _repoMock.Setup(r => r.ObtenerPorNombreAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);
            
        var caso = CrearCasoDeUso();
        
        // Asumiendo que tu DTO se instancia así. Si no compila, ajustalo a tu constructor.
        var request = new AuthenticarUsuarioRequest
        {
            NombreUsuario = "noexiste",
            Contrasena = "clave123"
        };

        // Act
        var resultado = await caso.EjecutarAsync(request, CancellationToken.None);

        // Assert
        Assert.False(resultado.IsSuccess); // Tu corrección
        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        Assert.Equal(MensajeCredencialesInvalidas, resultado.Error);
    }

    [Fact]
    public async Task Ejecutar_ContrasenaIncorrecta_DevuelveFalloGenerico()
    {
        // Arrange
        var usuario = new Usuario { NombreUsuario = "admin", HashContrasena = "hash_real", Activo = true };
        
        _repoMock.Setup(r => r.ObtenerPorNombreAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
        
        _hashMock.Setup(h => h.VerificarClave("clave_mala", "hash_real"))
            .Returns(false);
            
        var caso = CrearCasoDeUso();
        var request = new AuthenticarUsuarioRequest
        {
            NombreUsuario = "admin",
            Contrasena = "clave_mala"
        };

        // Act
        var resultado = await caso.EjecutarAsync(request, CancellationToken.None);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        Assert.Equal(MensajeCredencialesInvalidas, resultado.Error); // CA2 cumplido
    }

    [Fact]
    public async Task Ejecutar_UsuarioInactivo_DevuelveFallo()
    {
        // Arrange
        var usuario = new Usuario { NombreUsuario = "inactivo", HashContrasena = "hash_real", Activo = false };
        
        _repoMock.Setup(r => r.ObtenerPorNombreAsync("inactivo", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
            
        // ESTO FALTABA: Simulamos que la clave está bien para que el código llegue a evaluar el Activo = false
        _hashMock.Setup(h => h.VerificarClave("clave123", "hash_real"))
            .Returns(true);
            
        var caso = CrearCasoDeUso();
        var request = new AuthenticarUsuarioRequest { NombreUsuario = "inactivo", Contrasena = "clave123" };

        // Act
        var resultado = await caso.EjecutarAsync(request, CancellationToken.None);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        Assert.Equal(MensajeUsuarioInactivo, resultado.Error); 
    }

    [Fact]
    public async Task Ejecutar_CredencialesValidas_DevuelveExitoConToken()
    {
        // Arrange
        var usuario = new Usuario { Id = 1, NombreUsuario = "admin", HashContrasena = "hash_real", RolId = 2, SucursalId = 1, Activo = true };
        
        _repoMock.Setup(r => r.ObtenerPorNombreAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuario);
            
        _hashMock.Setup(h => h.VerificarClave("clave_buena", "hash_real"))
            .Returns(true);
            
        _tokenMock.Setup(t => t.GenerarToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<short>()))
            .Returns("token.jwt.valido");
            
        var caso = CrearCasoDeUso();
        var request = new AuthenticarUsuarioRequest{
            NombreUsuario = "admin",
            Contrasena = "clave_buena"
        };

        // Act
        var resultado = await caso.EjecutarAsync(request, CancellationToken.None);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal("token.jwt.valido", resultado.Value?.Token); // CA1 cumplido
    }
}
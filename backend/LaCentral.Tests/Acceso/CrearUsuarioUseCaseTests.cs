using Moq;
using Xunit;
using LaCentral.UseCases;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Models;

namespace LaCentral.Tests.Acceso;

public class CrearUsuarioUseCaseTests
{
    private readonly Mock<IUsuarioRepositorio> _repoMock = new();
    private readonly Mock<IServicioHash> _hashMock = new();

    private CrearUsuarioUseCase CrearCasoDeUso() =>
        new CrearUsuarioUseCase(_repoMock.Object, _hashMock.Object);

    // Método auxiliar para no repetir un request válido en cada test
    private CrearUsuarioRequest CrearRequestValido() => new CrearUsuarioRequest
    {
        NombreUsuario = "usuario.valido",
        Password = "clave123",
        Rol = "OPERADOR",
        Sucursal = "FR"
    };

    // Hallazgo 1: contraseña vacía o menor a 6 caracteres
    [Fact]
    public async Task Ejecutar_ContrasenaMenorAMinimo_DevuelveFalloInvalido()
    {
        // Arrange
        var caso = CrearCasoDeUso();
        var requestMalo = CrearRequestValido();
        requestMalo.Password = "1234"; // 4 caracteres, menor al mínimo de 6

        // Act
        var resultado = await caso.EjecutarAsync(requestMalo);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    // Hallazgo 2: nombre de usuario vacío
    [Fact]
    public async Task Ejecutar_NombreUsuarioVacio_DevuelveFalloInvalido()
    {
        // Arrange
        var caso = CrearCasoDeUso();
        var requestMalo = CrearRequestValido();
        requestMalo.NombreUsuario = "";

        // Act
        var resultado = await caso.EjecutarAsync(requestMalo);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    // Hallazgo 2: nombre de usuario que excede el largo máximo de la columna (varchar(30))
    [Fact]
    public async Task Ejecutar_NombreUsuarioExcedeMaximo_DevuelveFalloInvalido()
    {
        // Arrange
        var caso = CrearCasoDeUso();
        var requestMalo = CrearRequestValido();
        requestMalo.NombreUsuario = new string('A', 31); // límite real es 30

        // Act
        var resultado = await caso.EjecutarAsync(requestMalo);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    // Hallazgo 6: Sucursal y Rol se normalizan con el mismo criterio (Trim + ToUpperInvariant)
    [Fact]
    public async Task Ejecutar_SucursalYRolEnMinuscula_DevuelveExito()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteNombreUsuarioAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _hashMock.Setup(h => h.HashearClave(It.IsAny<string>()))
            .Returns("hash-simulado");

        Usuario? usuarioCreado = null;
        _repoMock.Setup(r => r.CrearAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, _) => usuarioCreado = u)
            .Returns(Task.CompletedTask);

        var caso = CrearCasoDeUso();
        var requestMinuscula = CrearRequestValido();
        requestMinuscula.Sucursal = "fr";
        requestMinuscula.Rol = "operador";

        // Act
        var resultado = await caso.EjecutarAsync(requestMinuscula);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.NotNull(usuarioCreado);
        Assert.Equal(1, usuarioCreado!.SucursalId); // FR -> 1
        Assert.Equal(1, usuarioCreado.RolId); // OPERADOR -> 1
    }
}

using Moq;
using Xunit;
using LaCentral.UseCases;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades; // Ajustá el namespace si tu entidad está en otro lado

namespace LaCentral.UseCases.Tests.Clientes;

public class DarDeBajaClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _repositorioMock;
    private readonly DarDeBajaClienteUseCase _useCase;

    public DarDeBajaClienteUseCaseTests()
    {
        _repositorioMock = new Mock<IClienteRepositorio>();
        _useCase = new DarDeBajaClienteUseCase(_repositorioMock.Object);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoClienteNoExiste_RetornaNoEncontrado()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        // Act
        var resultado = await _useCase.EjecutarAsync(1, "123", "Motivo");

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoClienteYaEstaInactivo_RetornaInvalido()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = false };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _useCase.EjecutarAsync(1, "123", "Motivo");

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoConfirmacionEsMala_RetornaInvalido()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = true, Cuit = "20123456789" };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act (Le pasamos un CUIT incorrecto)
        var resultado = await _useCase.EjecutarAsync(1, "CUIT-INCORRECTO", "Motivo");

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoNoTieneCuitYConfirmacionFallaPorCodigo_RetornaInvalido()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = true, Cuit = null, Codigo = "MULTISOFT-1" };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _useCase.EjecutarAsync(1, "CODIGO-MAL", "Motivo");

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoDatosSonValidos_ActualizaYRetornaExito()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = true, Cuit = "20123456789" };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _useCase.EjecutarAsync(1, "20123456789", "Cierre de empresa");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.False(cliente.Activo);
        Assert.Equal("Cierre de empresa", cliente.MotivoBaja);
        Assert.NotNull(cliente.FechaBaja);
        
        // Verifica que el repositorio se haya llamado una vez para guardar
        _repositorioMock.Verify(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>()), Times.Once);
    }
}
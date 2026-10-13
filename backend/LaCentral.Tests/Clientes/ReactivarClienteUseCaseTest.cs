using Moq;
using Xunit;
using LaCentral.UseCases;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Tests.Clientes;

public class ReactivarClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _repositorioMock;
    private readonly ReactivarClienteUseCase _useCase;

    public ReactivarClienteUseCaseTests()
    {
        _repositorioMock = new Mock<IClienteRepositorio>();
        _useCase = new ReactivarClienteUseCase(_repositorioMock.Object);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoClienteNoExiste_RetornaNoEncontrado()
    {
        // Arrange
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);

        // Act
        var resultado = await _useCase.EjecutarAsync(1);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoClienteYaEstaActivo_RetornaInvalido()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = true };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _useCase.EjecutarAsync(1);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task EjecutarAsync_CuandoEsInactivo_LoActivaYRetornaExito()
    {
        // Arrange
        var cliente = new Cliente { UsuarioAltaId = 1, Activo = false, MotivoBaja = "Temporal", FechaBaja = DateTime.UtcNow };
        _repositorioMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _useCase.EjecutarAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.True(cliente.Activo);
        // Opcional: si tu regla de negocio dice que hay que limpiar el motivo/fecha al reactivar, lo testeás acá.
        
        _repositorioMock.Verify(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>()), Times.Once);
    }
}
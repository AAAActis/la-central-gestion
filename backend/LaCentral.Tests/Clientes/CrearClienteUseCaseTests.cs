using Moq;
using Xunit;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.Tests.Clientes;

public class CrearClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _repoMock = new();
    private readonly Mock<IContextoUsuario> _contextoMock = new();

    private CrearClienteUseCase CrearCasoDeUso()
    {
        // Simulamos un operador válido para la trazabilidad
        _contextoMock.Setup(c => c.UsuarioId).Returns(1); 
        return new CrearClienteUseCase(_repoMock.Object, _contextoMock.Object);
    }

    // Método auxiliar para no repetir la creación de un DTO válido en cada test
    private CrearClienteRequest CrearRequestValido() => new CrearClienteRequest(
        "CLI-001", 
        "La Central S.A.", 
        "30111111118", 
        "Responsable Inscripto", 
        "Contado",
        new List<string> { "3511234567" }, 
        new List<string> { "Av. Colón 123" }
    );

    [Fact]
    public async Task Ejecutar_CamposObligatoriosVacios_DevuelveFalloInvalido()
    {
        // Arrange
        var caso = CrearCasoDeUso();
        // Pasamos vacío el código y la razón social
        var requestInvalido = new CrearClienteRequest("", "", null, "Resp. Inscripto", "Contado", new List<string>(), new List<string>());

        // Act
        var resultado = await caso.EjecutarAsync(requestInvalido);

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task Ejecutar_CodigoDuplicado_DevuelveConflicto()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var caso = CrearCasoDeUso();

        // Act
        var resultado = await caso.EjecutarAsync(CrearRequestValido());

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("código", resultado.Error);
    }

    [Fact]
    public async Task Ejecutar_RazonSocialRepetida_DevuelveExitoConAdvertencia()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
            
        // Simulamos que la razón social ya está registrada
        _repoMock.Setup(r => r.ExisteRazonSocialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
            
        var caso = CrearCasoDeUso();

        // Act
        var resultado = await caso.EjecutarAsync(CrearRequestValido());

        // Assert
        Assert.True(resultado.IsSuccess);
        // Este es el test picante que valida que se guarde igual, pero tire la advertencia
        Assert.NotNull(resultado.Value?.Advertencia);
        Assert.Contains("Advertencia", resultado.Value.Advertencia);
    }

    [Fact]
    public async Task Ejecutar_DatosValidos_DevuelveExitoSinAdvertencia()
    {
        // Arrange
        _repoMock.Setup(r => r.ExisteCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repoMock.Setup(r => r.ExisteRazonSocialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
            
        var caso = CrearCasoDeUso();

        // Act
        var resultado = await caso.EjecutarAsync(CrearRequestValido());

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Null(resultado.Value?.Advertencia);
        Assert.Equal("CLI-001", resultado.Value?.Codigo);
    }
}
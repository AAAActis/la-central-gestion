using Moq;
using Xunit;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Clientes;

public class ModificarClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _repoMock = new();

    private ModificarClienteUseCase CrearCasoDeUso() => new(_repoMock.Object);

    private ModificarClienteRequest CrearRequestValido() => new(
        "CLI-001", "Cliente Editado", "30-12345678-9", "RI", "Contado", null, null);

    private Cliente CrearClienteBdActivo() => new()
    {
        Codigo = "CLI-001", RazonSocial = "Original", Cuit = "30-00000000-0", Activo = true
    };

// Una dirección sin calle llega como "" (así la lee el repositorio): filtrarla la borraba con su localidad.
    [Fact]
    public async Task Ejecutar_DireccionSinCalle_SeConservaParaElRepositorio()
    {
        var cliente = CrearClienteBdActivo();
        cliente.Direcciones = new List<string> { "", "Av. Colón 100" };
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        _repoMock.Setup(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var request = CrearRequestValido() with { Cuit = cliente.Cuit, Direcciones = new List<string> { "", "Av. Colón 100" } };

        var resultado = await CrearCasoDeUso().EjecutarAsync(1, request);

        Assert.True(resultado.IsSuccess);
        _repoMock.Verify(r => r.ActualizarAsync(
            It.Is<Cliente>(c => c.Direcciones.SequenceEqual(new[] { "", "Av. Colón 100" })), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Ejecutar_ClienteInactivo_DevuelveInvalido()
    {
        var clienteInactivo = CrearClienteBdActivo();
        clienteInactivo.Activo = false;

        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(clienteInactivo);

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("dado de baja", resultado.Error);
    }

    [Fact]
    public async Task Ejecutar_CuitDeOtroCliente_DevuelveConflicto()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CrearClienteBdActivo());

        _repoMock.Setup(r => r.ObtenerPorCuitAsync("30-12345678-9", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Cliente { Codigo = "CLI-002", RazonSocial = "Otro Cliente", Cuit = "30-12345678-9" });

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("ya pertenece", resultado.Error);
    }

    [Fact]
    public async Task Ejecutar_CodigoDeOtroCliente_DevuelveConflictoSinActualizar()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CrearClienteBdActivo());
        _repoMock.Setup(r => r.ExisteCodigoAsync("CLI-002", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var resultado = await CrearCasoDeUso().EjecutarAsync(1, CrearRequestValido() with { Codigo = "CLI-002" });

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("CLI-002", resultado.Error);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Ejecutar_LaFilaDesaparecioAntesDeActualizar_DevuelveNoEncontrado()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CrearClienteBdActivo());
        _repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await CrearCasoDeUso().EjecutarAsync(1, CrearRequestValido() with { Cuit = null });

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }
}
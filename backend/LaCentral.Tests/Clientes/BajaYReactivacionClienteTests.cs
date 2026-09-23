using Moq;
using Xunit;
using LaCentral.UseCases;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using System.Threading;
using System.Threading.Tasks;

namespace LaCentral.Tests.Clientes;

public class BajaYReactivacionClienteTests
{
    private readonly Mock<IClienteRepositorio> _repoMock = new();

    private Cliente CrearClienteBase(bool activo = true) => new Cliente
    {
        Codigo = "CLI-001",
        Cuit = "30-12345678-9",
        Activo = activo,
        MotivoBaja = activo ? null : "Motivo histórico"
    };

    // --- TESTS HALLAZGO 8 ---

    [Fact]
    public async Task DarDeBaja_ClienteInexistente_AplicaHallazgo8_DevuelveNoEncontrado()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Cliente?)null);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(999, "30-12345678-9", "Cierre empresa");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reactivar_ClienteInexistente_AplicaHallazgo8_DevuelveNoEncontrado()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Cliente?)null);
        var casoUso = new ReactivarClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(999);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- TESTS DOD DAR DE BAJA ---

    [Fact]
    public async Task DarDeBaja_CamposValidos_DesactivaClienteYGuardaMotivo()
    {
        var cliente = CrearClienteBase(activo: true);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1, "30-12345678-9", "Cese de actividades");

        Assert.True(resultado.IsSuccess);
        Assert.False(cliente.Activo);
        Assert.Equal("Cese de actividades", cliente.MotivoBaja);
        _repoMock.Verify(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DarDeBaja_CuitReescritoIncorrecto_DevuelveInvalido()
    {
        var cliente = CrearClienteBase(activo: true);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1, "11-11111111-1", "Motivo X");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DarDeBaja_MotivoVacio_DevuelveInvalido()
    {
        var cliente = CrearClienteBase(activo: true);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1, "30-12345678-9", "");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DarDeBaja_ClienteYaInactivo_DevuelveInvalido()
    {
        var cliente = CrearClienteBase(activo: false);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1, "30-12345678-9", "Otro motivo");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // --- TESTS DOD REACTIVAR ---

    [Fact]
    public async Task Reactivar_ClienteInactivo_ActivaSinBorrarHistorial()
    {
        var cliente = CrearClienteBase(activo: false);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new ReactivarClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1);

        Assert.True(resultado.IsSuccess);
        Assert.True(cliente.Activo);
        Assert.NotNull(cliente.MotivoBaja); // Garantizamos CA-003: no se blanquea el historial
        _repoMock.Verify(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivar_ClienteYaActivo_DevuelveInvalido()
    {
        var cliente = CrearClienteBase(activo: true);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        var casoUso = new ReactivarClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
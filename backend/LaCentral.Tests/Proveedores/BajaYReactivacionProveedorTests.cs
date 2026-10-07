using Moq;
using Xunit;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using System.Threading;
using System.Threading.Tasks;

namespace LaCentral.Tests.Proveedores;

public class BajaYReactivacionProveedorTests
{
    private readonly Mock<IProveedorRepositorio> _repoMock = new();

    private Proveedor CrearProveedorBase(bool activo = true) => new Proveedor
    {
        Id = 1,
        Codigo = "PRV-001",
        Cuit = "30-12345678-9",
        Activo = activo,
        MotivoBaja = activo ? null : "Motivo histórico"
    };

    [Fact]
    public async Task DarDeBaja_ProveedorInexistente_AplicaHallazgo8_DevuelveNoEncontrado()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Proveedor?)null);
        var casoUso = new DarDeBajaProveedorUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(999, new DarDeBajaProveedorRequest("Cierre empresa", "PRV-001"));

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        _repoMock.Verify(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DarDeBaja_CamposValidos_DesactivaProveedorYGuardaMotivo()
    {
        var proveedor = CrearProveedorBase(activo: true);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);
        var casoUso = new DarDeBajaProveedorUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1, new DarDeBajaProveedorRequest("Cese de actividades", "PRV-001"));

        Assert.True(resultado.IsSuccess);
        Assert.False(proveedor.Activo);
        Assert.Equal("Cese de actividades", proveedor.MotivoBaja);
        _repoMock.Verify(r => r.ActualizarAsync(proveedor, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivar_ProveedorInactivo_ActivaSinBorrarHistorial()
    {
        var proveedor = CrearProveedorBase(activo: false);
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.ExisteCuitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var casoUso = new ReactivarProveedorUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(1);

        Assert.True(resultado.IsSuccess);
        Assert.True(proveedor.Activo);
        Assert.NotNull(proveedor.MotivoBaja);
        _repoMock.Verify(r => r.ActualizarAsync(proveedor, It.IsAny<CancellationToken>()), Times.Once);
    }
}
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

    [Fact]
    public async Task DarDeBaja_ClienteInexistente_AplicaHallazgo8_DevuelveNoEncontrado()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(999, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Cliente?)null);
        var casoUso = new DarDeBajaClienteUseCase(_repoMock.Object);

        var resultado = await casoUso.EjecutarAsync(999, "30-11111111-9", "Cierre empresa");

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
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
    }
}
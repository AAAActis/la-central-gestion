using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-012, el listado: lógica del caso de uso con Moq. El filtrado lo prueba PostgreSQL.</summary>
public class ConsultarNotasCreditoCompraUseCaseTests
{
    private readonly Mock<INotaCreditoCompraRepositorio> _notas = new();
    private FiltroNotasCreditoCompra? _pedido;

    private static readonly NotaCreditoCompraResumen Una =
        new(5, 7, "Proveedor", "0010-00004321", "BONIFICACION", new DateOnly(2026, 11, 5), 1000m, false);

    public ConsultarNotasCreditoCompraUseCaseTests() =>
        _notas.Setup(n => n.BuscarAsync(It.IsAny<FiltroNotasCreditoCompra>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((FiltroNotasCreditoCompra f, int _, int _, CancellationToken _) => _pedido = f)
            .ReturnsAsync((new[] { Una }, 31));

    private ConsultarNotasCreditoCompraUseCase Caso() => new(_notas.Object);

    private void NoBusco() => _notas.Verify(n => n.BuscarAsync(It.IsAny<FiltroNotasCreditoCompra>(), It.IsAny<int>(),
        It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task DevuelveLaPaginaDelRepositorio_ConElFiltroPedido()
    {
        var filtro = new FiltroNotasCreditoCompra(7, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30));

        var resultado = await Caso().EjecutarAsync(filtro, 2, 10);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal((31, 2, 10), (resultado.Value.Total, resultado.Value.Pagina, resultado.Value.TamanoPagina));
        Assert.Equal(Una, Assert.Single(resultado.Value.Items));
        Assert.Equal(filtro, _pedido);
        _notas.Verify(n => n.BuscarAsync(It.IsAny<FiltroNotasCreditoCompra>(), 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    // Como la 026: (pagina - 1) × tamaño desbordaría el int del Skip.
    [InlineData(30_000_000, 100)]
    public async Task PaginaInvalida_Invalido(int pagina, int tamano)
    {
        var resultado = await Caso().EjecutarAsync(new FiltroNotasCreditoCompra(), pagina, tamano);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoBusco();
    }

    [Fact]
    public async Task RangoDeEmisionInvertido_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new FiltroNotasCreditoCompra(EmisionDesde: new DateOnly(2026, 10, 31), EmisionHasta: new DateOnly(2026, 10, 1)));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("emisión", resultado.Error);
        NoBusco();
    }

    [Fact]
    public async Task UnRangoDeUnSoloDia_EsValido()
    {
        var dia = new DateOnly(2026, 10, 1);
        Assert.True((await Caso().EjecutarAsync(new FiltroNotasCreditoCompra(EmisionDesde: dia, EmisionHasta: dia))).IsSuccess);
    }

    [Fact]
    public async Task LaPrimeraPaginaDeCien_EsValida()
        => Assert.True((await Caso().EjecutarAsync(new FiltroNotasCreditoCompra(), 1, 100)).IsSuccess);
}

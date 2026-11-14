using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>HU-FCO-02 (026), lógica del caso de uso con Moq. El filtrado lo prueba PostgreSQL.</summary>
public class ConsultarFacturasCompraUseCaseTests
{
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();
    private FiltroFacturasCompra? _pedido;

    private static readonly FacturaCompraResumen Una =
        new(5, 7, "Proveedor", "A-0001-00000001", "A", new DateOnly(2026, 10, 1), null, false);

    public ConsultarFacturasCompraUseCaseTests() =>
        _facturas.Setup(f => f.BuscarAsync(It.IsAny<FiltroFacturasCompra>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((FiltroFacturasCompra f, int _, int _, CancellationToken _) => _pedido = f)
            .ReturnsAsync((new[] { Una }, 31));

    private ConsultarFacturasCompraUseCase Caso() => new(_facturas.Object);

    private void NoBusco() => _facturas.Verify(f => f.BuscarAsync(It.IsAny<FiltroFacturasCompra>(), It.IsAny<int>(),
        It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task DevuelveLaPaginaDelRepositorio_ConElNumeroSinEspacios()
    {
        var resultado = await Caso().EjecutarAsync(new FiltroFacturasCompra(7, "  A-0001-00000001 "), 2, 10);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal((31, 2, 10), (resultado.Value.Total, resultado.Value.Pagina, resultado.Value.TamanoPagina));
        Assert.Equal(Una, Assert.Single(resultado.Value.Items));
        Assert.Equal(new FiltroFacturasCompra(7, "A-0001-00000001"), _pedido);
        _facturas.Verify(f => f.BuscarAsync(It.IsAny<FiltroFacturasCompra>(), 2, 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnNumeroEnBlanco_NoFiltraPorNumero()
    {
        await Caso().EjecutarAsync(new FiltroFacturasCompra(7, "   "));

        Assert.Null(_pedido!.Numero);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task PaginaInvalida_Invalido(int pagina, int tamano)
    {
        var resultado = await Caso().EjecutarAsync(new FiltroFacturasCompra(), pagina, tamano);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoBusco();
    }

    // CA-003: un rango con el desde después del hasta no devuelve nada útil: se avisa.
    [Fact]
    public async Task RangoDeEmisionInvertido_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new FiltroFacturasCompra(EmisionDesde: new DateOnly(2026, 10, 31), EmisionHasta: new DateOnly(2026, 10, 1)));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("emisión", resultado.Error);
        NoBusco();
    }

    [Fact]
    public async Task RangoDeVencimientoInvertido_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new FiltroFacturasCompra(VencimientoDesde: new DateOnly(2026, 10, 31), VencimientoHasta: new DateOnly(2026, 10, 1)));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("vencimiento", resultado.Error);
        NoBusco();
    }

    [Fact]
    public async Task UnRangoDeUnSoloDia_EsValido()
    {
        var dia = new DateOnly(2026, 10, 1);
        Assert.True((await Caso().EjecutarAsync(new FiltroFacturasCompra(EmisionDesde: dia, EmisionHasta: dia, VencimientoDesde: dia, VencimientoHasta: dia))).IsSuccess);
    }
}

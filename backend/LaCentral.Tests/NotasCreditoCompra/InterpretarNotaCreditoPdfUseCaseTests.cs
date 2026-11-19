using LaCentral.Api.Pdf;
using LaCentral.Tests.FacturasCompra;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.NotasCreditoCompra;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-010 · interpretación del PDF de la nota de crédito, con dobles.</summary>
public class InterpretarNotaCreditoPdfUseCaseTests
{
    private const int Expoyer = 3;

    private readonly Mock<ILectorTextoPdf> _lector = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();

    public InterpretarNotaCreditoPdfUseCaseTests()
    {
        ElPdfDice(PdfSintetico.NotaCreditoExpoyer());
        ElProveedorDelCuitEs(new Proveedor { Id = Expoyer, RazonSocial = "Expoyer S.A.", Activo = true });
        LaFacturaEsta("0011-00001111", 501);
        LaFacturaEsta("0011-00002222", 502);
    }

    private InterpretarNotaCreditoPdfUseCase Caso() => new(_lector.Object, _proveedores.Object, _facturas.Object);

    private void ElPdfDice(byte[] pdf)
        => _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(new LectorTextoPdf().LeerRenglones(pdf));

    private void ElProveedorDelCuitEs(Proveedor? proveedor)
        => _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitProveedor, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);

    private void LaFacturaEsta(string numero, int id)
        => _facturas.Setup(f => f.ObtenerNumeroActivoAsync(Expoyer, numero, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FacturaCompraExistente(id, numero, new DateOnly(2027, 1, 10)));

    [Fact]
    public async Task Expoyer_ProponeLaNotaConSuProveedorYSusFacturas_SinNadaParaRevisar()
    {
        var resultado = await Caso().EjecutarAsync([1]);

        Assert.True(resultado.IsSuccess, resultado.Error);
        var n = resultado.Value;
        Assert.Equal(((int?)Expoyer, "0010-00004321", (DateOnly?)new DateOnly(2027, 2, 4), "BONIFICACION", (decimal?)1000m),
            (n.ProveedorId, n.Numero, n.FechaEmision, n.Clase, n.ImporteNeto));
        Assert.Equal([501, 502], n.FacturaIds);
        Assert.Empty(n.FacturasSinEncontrar);
        Assert.Empty(n.ParaRevisar);
    }

    [Fact]
    public async Task UnaFacturaAsociadaQueNoEstaCargada_SeInforma_YLasFacturasQuedanParaRevisar()
    {
        _facturas.Setup(f => f.ObtenerNumeroActivoAsync(Expoyer, "0011-00002222", It.IsAny<CancellationToken>()))
            .ReturnsAsync((FacturaCompraExistente?)null);

        var n = (await Caso().EjecutarAsync([1])).Value;

        Assert.Equal([501], n.FacturaIds);
        Assert.Equal(["0011-00002222"], n.FacturasSinEncontrar);
        Assert.Equal(["facturaIds"], n.ParaRevisar);
    }

    [Fact]
    public async Task SinFacturasAsociadasEnElTexto_FacturaIdsVaUnaSolaVezParaRevisar()
    {
        ElPdfDice(PdfSintetico.NotaCreditoExpoyer(docAsoc: ""));

        var n = (await Caso().EjecutarAsync([1])).Value;

        Assert.Empty(n.FacturaIds);
        Assert.Equal(["facturaIds"], n.ParaRevisar);
    }

    [Fact]
    public async Task SiLosDosCuitSonDeProveedores_MandaElPrimero_ElDelEmisor()
    {
        _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitCliente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 77, RazonSocial = "Otro", Activo = true });

        Assert.Equal(Expoyer, (await Caso().EjecutarAsync([1])).Value.ProveedorId);
    }

    [Fact]
    public async Task ElProveedorDelCuitDadoDeBaja_NoSeToma()
    {
        ElProveedorDelCuitEs(new Proveedor { Id = Expoyer, RazonSocial = "Expoyer S.A.", Activo = false, MotivoBaja = "x", FechaBaja = DateTime.UtcNow });

        var n = (await Caso().EjecutarAsync([1])).Value;

        Assert.Null(n.ProveedorId);
        Assert.Contains("proveedorId", n.ParaRevisar);
    }

    [Fact]
    public async Task SinProveedorReconocido_NoSeBuscanFacturas_YTodasQuedanSinEncontrar()
    {
        ElProveedorDelCuitEs(null);

        var n = (await Caso().EjecutarAsync([1])).Value;

        Assert.Null(n.ProveedorId);
        Assert.Empty(n.FacturaIds);
        Assert.Equal(["0011-00001111", "0011-00002222"], n.FacturasSinEncontrar);
        Assert.Equal(["proveedorId", "facturaIds"], n.ParaRevisar);
        _facturas.Verify(f => f.ObtenerNumeroActivoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoEsUnPdf_Invalido_ConLaCargaManual()
    {
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(Result<IReadOnlyList<string>>.Failure(TipoError.Invalido, "no es PDF"));

        await Rechaza("no es un PDF");
    }

    [Fact]
    public async Task Escaneado_Invalido_ConLaCargaManual()
    {
        ElPdfDice(PdfSintetico.SinTexto());

        await Rechaza("escaneado");
    }

    [Fact]
    public async Task UnaFactura_NoSeReconoceComoNotaDeCredito()
    {
        ElPdfDice(PdfSintetico.FacturaExpoyer());

        await Rechaza("no reconoce");
    }

    private async Task Rechaza(string motivo)
    {
        var resultado = await Caso().EjecutarAsync([1]);

        Assert.Equal((false, TipoError.Invalido), (resultado.IsSuccess, resultado.Tipo));
        Assert.Contains(motivo, resultado.Error);
        Assert.Contains("vía manual", resultado.Error);
    }
}

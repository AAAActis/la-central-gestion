using LaCentral.Api.Pdf;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>HU-FCO-05 (029): la propuesta para revisar, sin guardar nada ni crear artículos.</summary>
public class InterpretarFacturaPdfUseCaseTests
{
    private readonly Mock<ILectorTextoPdf> _lector = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();

    private InterpretarFacturaPdfUseCase CasoDeUso() => new(_lector.Object, _proveedores.Object, _facturas.Object);

    private void ElPdfDice(byte[] pdf)
        => _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(new LectorTextoPdf().LeerRenglones(pdf));

    private void ElProveedorDelCuitEs(Proveedor proveedor)
        => _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitProveedor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedor);

    [Fact]
    public async Task CA002_UnArchivoQueNoEsPdf_Invalido_YPideLaCargaManual()
    {
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>()))
            .Returns(Result<IReadOnlyList<string>>.Failure(TipoError.Invalido, "No es un PDF."));

        var resultado = await CasoDeUso().EjecutarAsync([1, 2, 3]);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("vía manual", resultado.Error);
    }

    [Fact]
    public async Task CA002_UnPdfEscaneadoSinTexto_Invalido_YPideLaCargaManual()
    {
        ElPdfDice(PdfSintetico.SinTexto());

        var resultado = await CasoDeUso().EjecutarAsync([1]);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("sin texto", resultado.Error);
        Assert.Contains("vía manual", resultado.Error);
    }

    [Fact]
    public async Task UnLayoutQueNoSeConoce_Invalido_YPideLaCargaManual()
    {
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>()))
            .Returns(Result<IReadOnlyList<string>>.Success(["Nota de crédito", "Total 10,00"]));

        var resultado = await CasoDeUso().EjecutarAsync([1]);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("formato", resultado.Error);
        Assert.Contains("vía manual", resultado.Error);
    }

    [Fact]
    public async Task CA001_YCA004_ElProveedorSaleDeSuCuit_YCadaLineaDeSuCodigo_LaQueNoCoincideQuedaSinArticulo()
    {
        ElPdfDice(PdfSintetico.FacturaEtman());
        ElProveedorDelCuitEs(new Proveedor { Id = 7, RazonSocial = "Ficticio", Activo = true });
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(7,
                It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "AB-100", "ZZ9" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["AB-100"] = 50 });

        var resultado = await CasoDeUso().EjecutarAsync([1]);

        Assert.True(resultado.IsSuccess, resultado.Error);
        var f = resultado.Value;
        Assert.Equal((7, "A", "0042-00001234"), (f.ProveedorId, f.TipoComprobante, f.Numero));
        Assert.Equal((new DateOnly(2027, 2, 3), new DateOnly(2027, 3, 10)), (f.FechaEmision, f.FechaVencimiento));
        Assert.Equal((2000.25m, 1000.50m), (f.PercepcionIva, f.PercepcionIibb));
        Assert.Equal([(int?)50, null], f.Lineas.Select(l => l.ArticuloId));
        Assert.Equal(("ZZ9", 1250.50m, 10m), (f.Lineas[1].CodigoProveedor, f.Lineas[1].PrecioUnitario, f.Lineas[1].DescuentoPorcentaje));
        Assert.Equal((5m, 20m), (f.Lineas[0].Descuento2Porcentaje, f.Lineas[0].Descuento3Porcentaje));
        Assert.Empty(f.ParaRevisar);
    }

    [Fact]
    public async Task CA003_InterpretarNoEscribeNada_SoloBuscaLosCodigos()
    {
        ElPdfDice(PdfSintetico.FacturaEtman());
        ElProveedorDelCuitEs(new Proveedor { Id = 7, Activo = true });
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(7, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        await CasoDeUso().EjecutarAsync([1]);

        _facturas.Verify(f => f.ObtenerArticulosPorCodigoProveedorAsync(7, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        _facturas.VerifyNoOtherCalls();
        _proveedores.Verify(p => p.AgregarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
        _proveedores.Verify(p => p.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SinProveedorActivoConEseCuit_NoLoAdivina_QuedaParaRevisar_YNingunaLineaSeResuelve()
    {
        ElPdfDice(PdfSintetico.FacturaEtman());
        ElProveedorDelCuitEs(new Proveedor { Id = 7, Activo = false });

        var resultado = await CasoDeUso().EjecutarAsync([1]);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Null(resultado.Value.ProveedorId);
        Assert.Equal(["proveedorId"], resultado.Value.ParaRevisar);
        Assert.All(resultado.Value.Lineas, l => Assert.Null(l.ArticuloId));
        _facturas.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task LoQueElLayoutMarcaParaRevisar_LlegaALaPropuesta()
    {
        ElPdfDice(PdfSintetico.PresupuestoFazio());

        var resultado = await CasoDeUso().EjecutarAsync([1]);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(["numero", "proveedorId"], resultado.Value.ParaRevisar);
    }

    [Fact]
    public async Task ElNombreDelArchivoLlegaAlLayout()
    {
        ElPdfDice(PdfSintetico.PresupuestoFazio());

        var resultado = await CasoDeUso().EjecutarAsync([1], "Presupuesto_555.pdf");

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal("99-555", resultado.Value.Numero);
    }

    [Fact]
    public async Task ConUnProveedorElegido_LosCodigosSeBuscanEnEse_AunqueElCuitDelPdfSeaDeOtro()
    {
        ElPdfDice(PdfSintetico.FacturaEtman());
        ElProveedorDelCuitEs(new Proveedor { Id = 3, Activo = true });
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 7, RazonSocial = "Elegido", Activo = true });
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(3, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["ZZ9"] = 99 });
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(7,
                It.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(new[] { "AB-100", "ZZ9" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int> { ["ZZ9"] = 51 });

        var resultado = await CasoDeUso().EjecutarAsync([1], proveedorElegido: 7);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(7, resultado.Value.ProveedorId);
        Assert.Equal([null, (int?)51], resultado.Value.Lineas.Select(l => l.ArticuloId));
        Assert.DoesNotContain("proveedorId", resultado.Value.ParaRevisar);
        _facturas.Verify(f => f.ObtenerArticulosPorCodigoProveedorAsync(3, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConUnProveedorElegidoQueNoExisteOEstaInactivo_Invalido_SinBuscarCodigos(bool existe)
    {
        ElPdfDice(PdfSintetico.PresupuestoFazio());
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existe ? new Proveedor { Id = 7, RazonSocial = "Dado de baja", Activo = false } : null);
        // Si el caso de uso siguiera de largo, la búsqueda de códigos respondería: falla la aserción, no una excepción.
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());

        var resultado = await CasoDeUso().EjecutarAsync([1], proveedorElegido: 7);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _facturas.VerifyNoOtherCalls();
    }
}

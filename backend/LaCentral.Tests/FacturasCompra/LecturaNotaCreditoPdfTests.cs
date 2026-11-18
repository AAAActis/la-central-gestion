using LaCentral.Api.Pdf;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-07 (057) CA-010: el texto de la nota de crédito de Expoyer y su interpretación, sobre
/// PDF sintéticos con datos inventados. Sin base.
/// </summary>
public class LecturaNotaCreditoPdfTests
{
    private static IReadOnlyList<string> Renglones(byte[] pdf)
    {
        var leido = new LectorTextoPdf().LeerRenglones(pdf);
        Assert.True(leido.IsSuccess, leido.Error);
        return leido.Value;
    }

    private static NotaCreditoLeida Leer(byte[] pdf)
    {
        var leida = LayoutsPdf.LeerNotaCredito(Renglones(pdf));
        Assert.NotNull(leida);
        return leida;
    }

    [Fact]
    public void Expoyer_LeeNumeroFechaClaseNetoYFacturasAsociadas_SinNadaParaRevisar()
    {
        var leida = Leer(PdfSintetico.NotaCreditoExpoyer());

        Assert.Equal(("0010-00004321", (DateOnly?)new DateOnly(2027, 2, 4), "BONIFICACION", (decimal?)1000m),
            (leida.Numero, leida.FechaEmision, leida.Clase, leida.ImporteNeto));
        Assert.Equal(["0011-00001111", "0011-00002222"], leida.FacturasAsociadas);
        Assert.Equal([PdfSintetico.CuitProveedor, PdfSintetico.CuitCliente], leida.Cuits);
        Assert.Empty(leida.ParaRevisar);
    }

    [Fact]
    public void Expoyer_ElNetoVieneEnFormatoDeEeUu()
        => Assert.Equal(115040m, Leer(PdfSintetico.NotaCreditoExpoyer(subtotal: "115,040.00", total: "139,198.40")).ImporteNeto);

    [Fact]
    public void Expoyer_SinFacturasAsociadas_QuedanParaRevisar()
    {
        var leida = Leer(PdfSintetico.NotaCreditoExpoyer(docAsoc: ""));

        Assert.Empty(leida.FacturasAsociadas);
        Assert.Equal(["facturaIds"], leida.ParaRevisar);
    }

    [Fact]
    public void Expoyer_UnaClaseQueNoEsBonificacion_NoSeAdivina_QuedaParaRevisar()
    {
        // Solo hay una nota real, por bonificación: otra clase no se supone.
        var leida = Leer(PdfSintetico.NotaCreditoExpoyer(clase: "Devoluc"));

        Assert.Null(leida.Clase);
        Assert.Equal(["clase"], leida.ParaRevisar);
    }

    [Fact]
    public void Expoyer_SiElTotalNoEsElNetoMasElIva_ElImporteQuedaParaRevisar()
        => Assert.Equal(["importeNeto"], Leer(PdfSintetico.NotaCreditoExpoyer(total: "1,300.00")).ParaRevisar);

    [Fact]
    public void Expoyer_SiElTextoMencionaPercepciones_QuedanParaRevisar()
        => Assert.Equal(["percepciones"], Leer(PdfSintetico.NotaCreditoExpoyer(percepcion: "Percepcion IIBB 10.00")).ParaRevisar);

    [Fact]
    public void UnaFacturaDeExpoyer_NoEsUnaNotaDeCredito()
        => Assert.Null(LayoutsPdf.LeerNotaCredito(Renglones(PdfSintetico.FacturaExpoyer())));

    [Fact]
    public void LaNotaDeCredito_NoSeLeeComoFactura()
        => Assert.Null(LayoutsPdf.Leer(Renglones(PdfSintetico.NotaCreditoExpoyer())));
}

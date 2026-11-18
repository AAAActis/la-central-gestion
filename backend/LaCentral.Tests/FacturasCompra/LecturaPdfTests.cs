using LaCentral.Api.Pdf;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-05 (029) CA-001 y CA-002: el texto de un PDF y su interpretación por layout, sobre
/// PDF sintéticos con datos inventados. Sin base.
/// </summary>
public class LecturaPdfTests
{
    private static IReadOnlyList<string> Renglones(byte[] pdf)
    {
        var leido = new LectorTextoPdf().LeerRenglones(pdf);
        Assert.True(leido.IsSuccess, leido.Error);
        return leido.Value;
    }

    private static ComprobanteLeido Leer(byte[] pdf, string? nombreArchivo = null)
        => LayoutsPdf.Leer(Renglones(pdf), nombreArchivo) ?? throw new Xunit.Sdk.XunitException("El layout no se reconoció.");

    [Fact]
    public void Lector_DevuelveLosRenglonesDeArribaHaciaAbajo_YLasPalabrasDeIzquierdaADerecha()
    {
        var pdf = PdfSintetico.Generar([(300, "derecha"), (20, "izquierda dos")], [(20, "abajo")]);

        Assert.Equal(["izquierda dos derecha", "abajo"], Renglones(pdf));
    }

    [Fact]
    public void Lector_PalabrasDelMismoRenglonConLineasDeBaseApenasDistintas_VanJuntasYDeIzquierdaADerecha()
        => Assert.Equal(["izquierda derecha", "abajo"], Renglones(PdfSintetico.RenglonTorcido()));

    [Fact]
    public void Lector_UnArchivoQueNoEsPdf_Invalido()
    {
        var leido = new LectorTextoPdf().LeerRenglones("no soy un PDF"u8.ToArray());

        Assert.Equal(TipoError.Invalido, leido.Tipo);
    }

    [Fact]
    public void Lector_UnPdfSinTexto_NoDevuelveRenglones()
        => Assert.Empty(Renglones(PdfSintetico.SinTexto()));

    [Fact]
    public void Layouts_UnTextoQueNoEsDeNingunLayoutConocido_Null()
        => Assert.Null(LayoutsPdf.Leer(["Factura de un proveedor nuevo", "Total 10,00"]));

    [Fact]
    public void Etman_LeeLaCabecera_ConLosNumerosEnFormatoEstadounidense()
    {
        var leido = Leer(PdfSintetico.FacturaEtman());

        Assert.Equal([PdfSintetico.CuitProveedor, PdfSintetico.CuitCliente], leido.Cuits);
        Assert.Equal(("A", "0042-00001234"), (leido.TipoComprobante, leido.Numero));
        Assert.Equal((new DateOnly(2027, 2, 3), new DateOnly(2027, 3, 10)), (leido.FechaEmision, leido.FechaVencimiento));
        Assert.Equal((2000.25m, 1000.50m), (leido.PercepcionIva, leido.PercepcionIibb));
        Assert.Null(leido.PercepcionMunicipal);
        Assert.Null(leido.PercepcionGanancias);
        Assert.Empty(leido.ParaRevisar);
    }

    [Fact]
    public void Etman_LeeLasLineas_ConSusTresDescuentosEnPositivo_YSaltaLaContinuacionDeLaDescripcion()
    {
        var leido = Leer(PdfSintetico.FacturaEtman());

        Assert.Equal(
            [
                new LineaLeida("AB-100", "PASTILLA FICTICIA DELANTERA MARCAX", 2m, 4000m, 0m, 5m, 20m),
                new LineaLeida("ZZ9", "FILTRO FICTICIO MARCAY", 1m, 1250.50m, 10m, 0m, 0m),
            ],
            leido.Lineas);
    }

    [Theory]
    [InlineData("06", "B")]
    [InlineData("01", "A")]
    public void Etman_ElTipoSaleDelCodigoDeComprobante(string codigoAfip, string tipo)
        => Assert.Equal(tipo, Leer(PdfSintetico.FacturaEtman(codigoAfip)).TipoComprobante);

    [Fact]
    public void Etman_UnCodigoDeComprobanteQueNoEsAniB_NoAdivinaElTipo_YLoMarcaParaRevisar()
    {
        var leido = Leer(PdfSintetico.FacturaEtman(codigoAfip: "03"));

        Assert.Null(leido.TipoComprobante);
        Assert.Equal(["tipoComprobante"], leido.ParaRevisar);
    }

    [Fact]
    public void Etman_UnaLineaCuyoTotalNoCierraConSusDescuentos_SeMarcaParaRevisar()
    {
        var leido = Leer(PdfSintetico.FacturaEtman(totalPrimeraLinea: "6,000.00"));

        Assert.Equal(["lineas[0]"], leido.ParaRevisar);
    }

    [Theory]
    [InlineData("", "2,000.25", "percepcionIibb")]
    [InlineData("1,000.50", "", "percepcionIva")]
    [InlineData("1.000,50", "2,000.25", "percepcionIibb")]
    [InlineData("1,000.50", "2.000,25", "percepcionIva")]
    public void Etman_UnaPercepcionDelBloqueQueNoSePuedeLeer_QuedaNull_YSeMarcaParaRevisar(string iibb, string iva, string campo)
    {
        var leido = Leer(PdfSintetico.FacturaEtman(percepcionIibb: iibb, percepcionIva: iva));

        Assert.Null(campo == "percepcionIibb" ? leido.PercepcionIibb : leido.PercepcionIva);
        Assert.Equal([campo], leido.ParaRevisar);
    }

    [Fact]
    public void Fazio_LeeElPresupuesto_ConLosNumerosEnFormatoArgentino_YElDescuentoPorPartes()
    {
        var leido = Leer(PdfSintetico.PresupuestoFazio());

        Assert.Equal([PdfSintetico.CuitCliente], leido.Cuits);
        Assert.Equal(new DateOnly(2027, 2, 5), leido.FechaEmision);
        Assert.Null(leido.FechaVencimiento);
        Assert.Null(leido.PercepcionIva);
        Assert.Equal([new LineaLeida("QQ123", "Campana ficticia", 2m, 10000m, 28m, 22m, 0m)], leido.Lineas);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("factura.pdf")]
    [InlineData("Presupuesto_.pdf")]
    [InlineData("copia_Presupuesto_12345.pdf")]   // el nombre tiene que empezar con «Presupuesto_»
    public void Fazio_SinElNumeroEnElNombreDelArchivo_EsB_YElNumeroQuedaParaRevisar(string? nombreArchivo)
    {
        var leido = Leer(PdfSintetico.PresupuestoFazio(), nombreArchivo);

        // D-S6-13: lo comprado en negro es B; ya no es una suposición que haya que revisar.
        Assert.Equal("B", leido.TipoComprobante);
        Assert.Null(leido.Numero);
        Assert.Equal(["numero"], leido.ParaRevisar);
    }

    [Theory]
    [InlineData("Presupuesto_12345.pdf", "99-12345")]
    [InlineData("presupuesto_00777.PDF", "99-00777")]
    [InlineData("descargas/Presupuesto_4242.pdf", "99-4242")]   // algunos navegadores mandan la ruta
    public void Fazio_ElNumeroSaleDelNombreDelArchivo_ConPrefijo99_YQuedaParaRevisar(string nombreArchivo, string numero)
    {
        var leido = Leer(PdfSintetico.PresupuestoFazio(), nombreArchivo);

        Assert.Equal(("B", numero), (leido.TipoComprobante, leido.Numero));
        Assert.Equal(["numero"], leido.ParaRevisar);
    }

    [Fact]
    public void Fazio_ConMasDeTresDescuentos_UsaElCombinado_YMarcaLaLineaParaRevisar()
    {
        var leido = Leer(PdfSintetico.PresupuestoFazio(partes: "10+10+10+5"));

        Assert.Equal((43.84m, 0m, 0m), (leido.Lineas[0].Descuento1, leido.Lineas[0].Descuento2, leido.Lineas[0].Descuento3));
        Assert.Contains("lineas[0]", leido.ParaRevisar);
    }
}

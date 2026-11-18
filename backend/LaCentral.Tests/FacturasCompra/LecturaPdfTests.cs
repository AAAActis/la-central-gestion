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
        Assert.Equal(["lineas[0].descuento4", "lineas[1].descuento4"], leido.ParaRevisar);
    }

    [Fact]
    public void Etman_ProponeElPagoContadoComoCuartoDescuentoDeCadaLinea_YLoMarcaParaRevisar()
    {
        // D-S6-15: el 5 % de pago contado no viene en las líneas, sale del renglón «Pago Contado 5%».
        var leido = Leer(PdfSintetico.FacturaEtman());

        Assert.All(leido.Lineas, l => Assert.Equal(5m, l.Descuento4));
        Assert.Contains("lineas[0].descuento4", leido.ParaRevisar);
        Assert.Contains("lineas[1].descuento4", leido.ParaRevisar);
    }

    [Fact]
    public void Etman_PagoContadoConDecimales_LoProponeCompleto()
    {
        var renglones = Renglones(PdfSintetico.FacturaEtman())
            .Select(r => r.StartsWith("Pago Contado") ? r.Replace("5%", "2.5%") : r).ToList();

        var leido = LayoutsPdf.Leer(renglones)!;

        Assert.All(leido.Lineas, l => Assert.Equal(2.5m, l.Descuento4));
        Assert.Contains("lineas[0].descuento4", leido.ParaRevisar);
    }

    [Fact]
    public void Etman_SinElRenglonDePagoContado_NoProponeCuartoDescuento()
    {
        var renglones = Renglones(PdfSintetico.FacturaEtman()).Where(r => !r.StartsWith("Pago Contado")).ToList();

        var leido = LayoutsPdf.Leer(renglones)!;

        Assert.All(leido.Lineas, l => Assert.Equal(0m, l.Descuento4));
        Assert.Empty(leido.ParaRevisar);
    }

    [Fact]
    public void Etman_LeeLasLineas_ConSusTresDescuentosEnPositivo_YSaltaLaContinuacionDeLaDescripcion()
    {
        var leido = Leer(PdfSintetico.FacturaEtman());

        Assert.Equal(
            [
                new LineaLeida("AB-100", "PASTILLA FICTICIA DELANTERA MARCAX", 2m, 4000m, 0m, 5m, 20m, 5m),
                new LineaLeida("ZZ9", "FILTRO FICTICIO MARCAY", 1m, 1250.50m, 10m, 0m, 0m, 5m),
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
        Assert.Equal(["tipoComprobante"], leido.ParaRevisar.Where(c => !c.EndsWith(".descuento4")));
    }

    [Fact]
    public void Etman_UnaLineaCuyoTotalNoCierraConSusDescuentos_SeMarcaParaRevisar()
    {
        var leido = Leer(PdfSintetico.FacturaEtman(totalPrimeraLinea: "6,000.00"));

        Assert.Equal(["lineas[0]"], leido.ParaRevisar.Where(c => !c.EndsWith(".descuento4")));
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
        Assert.Equal([campo], leido.ParaRevisar.Where(c => !c.EndsWith(".descuento4")));
    }

    [Fact]
    public void Expoyer_LeeLaCabecera_SinTomarElCuitDelClienteNiInventarElVencimiento()
    {
        var leido = Leer(PdfSintetico.FacturaExpoyer());

        // El CUIT del cliente viene sin guiones: no se toma. «CUENTA CORRIENTE 35 DIAS» no es una fecha.
        Assert.Equal([PdfSintetico.CuitProveedor], leido.Cuits);
        Assert.Equal(("A", "0012-00001234"), (leido.TipoComprobante, leido.Numero));
        Assert.Equal(new DateOnly(2027, 2, 3), leido.FechaEmision);
        Assert.Null(leido.FechaVencimiento);
        Assert.Null(leido.PercepcionIva);
        Assert.Null(leido.PercepcionIibb);
        Assert.Empty(leido.ParaRevisar);
    }

    [Fact]
    public void Expoyer_LeeLasDosColumnas_ConLosPreciosYaNetos_YLosCodigosConEspacios()
    {
        var leido = Leer(PdfSintetico.FacturaExpoyer());

        Assert.Equal(
            [
                new LineaLeida("AB-100", "PASTILLA FICTICIA", 2m, 1000m, 0m, 0m, 0m),
                new LineaLeida("C 1234 5- F", "BOMBA FICTICIA 6325", 1m, 1250.50m, 0m, 0m, 0m),
                new LineaLeida("ZZ9", "FILTRO FICTICIO", 3m, 100m, 0m, 0m, 0m),
            ],
            leido.Lineas);
    }

    [Theory]
    [InlineData("6", "B")]
    [InlineData("1", "A")]
    public void Expoyer_ElTipoSaleDelCodigoDeComprobante(string codigoAfip, string tipo)
        => Assert.Equal(tipo, Leer(PdfSintetico.FacturaExpoyer(codigoAfip)).TipoComprobante);

    [Fact]
    public void Expoyer_UnCodigoDeComprobanteQueNoEsAniB_NoAdivinaElTipo_YLoMarcaParaRevisar()
    {
        var leido = Leer(PdfSintetico.FacturaExpoyer("3"));

        Assert.Null(leido.TipoComprobante);
        Assert.Equal(["tipoComprobante"], leido.ParaRevisar);
    }

    [Fact]
    public void Expoyer_UnaLineaCuyoTotalNoCierra_SeMarcaParaRevisar()
        => Assert.Equal(["lineas[2]"], Leer(PdfSintetico.FacturaExpoyer(totalTercera: "310.00")).ParaRevisar);

    [Fact]
    public void Expoyer_UnaPercepcionQueElLectorNoSabeLeer_MarcaLasPercepcionesParaRevisar()
    {
        var leido = Leer(PdfSintetico.FacturaExpoyer(percepcion: "Percepcion IIBB Cordoba 35.50"));

        Assert.Null(leido.PercepcionIibb);
        Assert.Equal(["percepcionIva", "percepcionIibb"], leido.ParaRevisar);
    }

    private const string EncabezadoExpoyer = "Cant. Codigo Descripcion P.Unit. Total Cant. Codigo Descripcion P.Unit. Total";

    [Theory]
    [InlineData("1 ZZ FILTRO 1.00 1.00", "ZZ", "FILTRO")]               // un código sin dígitos es el primer token
    [InlineData("2 AB-1 6325 100.00 200.00", "AB-1", "6325")]           // siempre queda una palabra de descripción
    public void Expoyer_ElCodigoEsElPrimerTokenYLosQueSiguenConDigitoOUnaLetra(string renglon, string codigo, string descripcion)
    {
        var leido = LayoutsPdf.Leer([EncabezadoExpoyer, renglon]);

        Assert.NotNull(leido);
        Assert.Equal((codigo, descripcion), (leido.Lineas[0].CodigoProveedor, leido.Lineas[0].Descripcion));
    }

    [Fact]
    public void Expoyer_ConElSignoDeNumeroYLasTildesDelTextoReal()
    {
        // El PDF sintético no puede escribir «º» ni tildes; el texto real del PDF las trae.
        var leido = LayoutsPdf.Leer([
            "FACTURA Nº 0012-00001234",
            "Código Identificatorio del Tipo de Comprobante. Código Nº 6 FECHA: 03/02/2027",
            "Cant. Código Descripción P.Unit. Total Cant. Código Descripción P.Unit. Total",
            "1 AB-1 PASTILLA 1.00 1.00",
        ]);

        Assert.NotNull(leido);
        Assert.Equal(("B", "0012-00001234", new DateOnly(2027, 2, 3)), (leido.TipoComprobante, leido.Numero, leido.FechaEmision));
    }

    [Fact]
    public void Expoyer_SinNumeroNiFecha_LosMarcaParaRevisar()
    {
        var leido = LayoutsPdf.Leer(["Comprobante. Codigo No 1", EncabezadoExpoyer, "1 AB-1 PASTILLA 1.00 1.00"]);

        Assert.NotNull(leido);
        Assert.Equal(("A", null, null), (leido.TipoComprobante, leido.Numero, leido.FechaEmision));
        Assert.Equal(["numero", "fechaEmision"], leido.ParaRevisar);
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

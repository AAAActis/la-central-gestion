using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-05 (029): PDF sintéticos que imitan el layout de cada proveedor, con datos
/// INVENTADOS (proveedor, CUIT, códigos y montos). Los PDF reales no entran al repo ni a los
/// tests (`docs/sprint06/ESTADO.md`, «Compuerta C7»).
/// </summary>
public static class PdfSintetico
{
    public const string CuitProveedor = "30-11111111-2";
    public const string CuitCliente = "30-22222222-3";

    /// <summary>Cada renglón: celdas (x, texto) a la misma altura, de arriba hacia abajo.</summary>
    public static byte[] Generar(params (double X, string Texto)[][] renglones)
    {
        var documento = new PdfDocumentBuilder();
        var fuente = documento.AddStandard14Font(Standard14Font.Helvetica);
        var pagina = documento.AddPage(PageSize.A4);
        var y = 800d;
        foreach (var renglon in renglones)
        {
            foreach (var (x, texto) in renglon)
                pagina.AddText(texto, 7, new PdfPoint(x, y), fuente);
            y -= 12;
        }
        return documento.Build();
    }

    /// <summary>
    /// Un renglón visual cuyas palabras no comparten la línea de base: la de la izquierda va
    /// 0,8 puntos más abajo y se escribe después, como pasa con PDF generados por drivers de impresora.
    /// </summary>
    public static byte[] RenglonTorcido()
    {
        var documento = new PdfDocumentBuilder();
        var fuente = documento.AddStandard14Font(Standard14Font.Helvetica);
        var pagina = documento.AddPage(PageSize.A4);
        pagina.AddText("derecha", 7, new PdfPoint(300, 800), fuente);
        pagina.AddText("izquierda", 7, new PdfPoint(20, 799.2), fuente);
        pagina.AddText("abajo", 7, new PdfPoint(20, 788), fuente);
        return documento.Build();
    }

    /// <summary>Un PDF con una página sin texto, como un escaneado.</summary>
    public static byte[] SinTexto()
    {
        var documento = new PdfDocumentBuilder();
        documento.AddPage(PageSize.A4).DrawRectangle(new PdfPoint(50, 50), 100, 100);
        return documento.Build();
    }

    private static (double, string)[] R(params (double, string)[] celdas) => celdas;

    /// <summary>
    /// Las fuentes estándar de PdfPig no escriben acentos: los encabezados van sin tilde, y los
    /// lectores no dependen de ellas.
    /// Factura con el layout de ETMAN: números en formato EE. UU., tres descuentos con signo
    /// menos y la descripción a veces en dos renglones.
    /// </summary>
    public static byte[] FacturaEtman(string codigoAfip = "01", string totalPrimeraLinea = "6,080.00",
        string percepcionIibb = "1,000.50", string percepcionIva = "2,000.25") => Generar(
        R((20, "REPUESTOS FICTICIOS S.A."), (300, "FACTURA")),
        R((300, $"Codigo Nro.: {codigoAfip} 0042- 00001234")),
        R((20, "Inicio de Actividades: 01/01/2001"), (300, $"C.U.I.T.: {CuitProveedor}")),
        R((20, "CLIENTE DE PRUEBA SAS"), (300, "CLIENTE"), (400, "FECHA")),
        R((20, "CALLE FALSA 123")),
        R((300, "555"), (400, "03/02/2027")),
        R((20, $"I.V.A: Responsable Inscripto C.U.I.T {CuitCliente}")),
        R((20, "Codigo"), (60, "Descripcion"), (200, "Marca"), (250, "Cantidad"), (300, "Precio"),
          (350, "Bonif."), (390, "Bonif."), (430, "Dto."), (480, "Total")),
        R((20, "AB-100"), (60, "PASTILLA FICTICIA DELANTERA"), (200, "MARCAX"), (250, "2"), (300, "4,000.00"),
          (350, "0.00"), (390, "-5.00"), (430, "-20.00"), (480, totalPrimeraLinea)),
        R((20, "ZZ9"), (60, "FILTRO FICTICIO"), (200, "MARCAY"), (250, "1"), (300, "1,250.50"),
          (350, "-10.00"), (390, "0.00"), (430, "0.00"), (480, "1,125.45")),
        R((60, "CONTINUACION DE LA DESCRIPCION")),
        // El bloque de percepciones imita el texto que PdfPig saca de la factura real: el detalle
        // por jurisdicción («Cordoba : …», sin separador de miles) lejos del total, y los totales
        // de las percepciones en el mismo renglón que el código de barras.
        R((20, "Percepciones IIBB"), (150, "NVT000-000001")),
        R((20, "Quincena + 35 dias"), (200, "VENCIMIENTOS Y DESCUENTOS VIGENTES")),
        R((20, "Cordoba : 1000.50")),
        R((20, "Pago Contado 5%"), (150, "$"), (200, "6,845.68"), (300, "Vto. 01/03/2027")),
        R((20, "Vencimiento QNA+35 Neto $"), (200, "7,205.45"), (300, "Vto. 10/03/2027")),
        R((300, "Subtotal : 4,264.51")),
        R((300, "I.V.A (21%) 895.55")),
        R((20, "0A1B2C3D4E5F60718293A4B5C6D7E8F9"), (300, $"Percepcion IIBB: {percepcionIibb}")),
        R((20, "F9E8D7C6B5A4938271605F4E3D2C1B0A"), (300, $"Percepcion de IVA {percepcionIva}")),
        R((300, "Total : 7,205.45")),
        R((300, "C.A.E.: 11111111111111 Fecha Vencimiento: 13/02/2027")));

    /// <summary>
    /// Presupuesto con el layout de Fazio: números en formato argentino, descuento combinado
    /// con sus partes entre paréntesis, sin número de comprobante ni CUIT del emisor.
    /// </summary>
    public static byte[] PresupuestoFazio(string partes = "28+22") => Generar(
        R((20, "*** Presupuesto")),
        R((20, "Cliente: 9999 - CLIENTE DE PRUEBA S.A.S."), (400, "Fecha : 05/02/2027")),
        R((20, $"CUIT: {CuitCliente}")),
        R((20, "Ord."), (40, "Cod. de Fab."), (100, "Descrip."), (250, "Cant."), (280, "Remito"),
          (330, "P. Lista"), (380, "% Desc."), (470, "P. Final"), (530, "Total")),
        R((20, "1"), (40, "QQ123"), (100, "Campana ficticia"), (250, "2"), (280, "900001 -"),
          (330, "10.000,00"), (380, $"43,84 ({partes})"), (470, "5.616,00"), (530, "11.232,00")),
        R((280, "05/02/2027")),
        R((100, "Segunda linea de la descripcion")),
        R((20, "Subtotal"), (250, "2"), (530, "11.232,00")));
}

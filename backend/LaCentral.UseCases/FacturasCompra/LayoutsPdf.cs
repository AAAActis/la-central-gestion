using System.Globalization;
using System.Text.RegularExpressions;
using LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>
/// HU-FCO-05 (029): un lector por reglas para cada layout de proveedor conocido (decisión del
/// 17/11/2026, `docs/sprint06/ESTADO.md`, «Compuerta C7»). Cada uno reconoce su layout por el
/// encabezado de columnas y declara su formato de número. Un proveedor nuevo es un método más.
/// Lo que un lector no encuentra o no puede afirmar va a ParaRevisar: nunca se adivina.
/// </summary>
public static class LayoutsPdf
{
    private static readonly Func<IReadOnlyList<string>, string?, ComprobanteLeido?>[] Layouts = [LeerEtman, LeerFazio, LeerExpoyer];

    private static readonly Regex Cuit = new(@"\b\d{2}-\d{8}-\d\b");
    private static readonly Regex Fecha = new(@"\b\d{2}/\d{2}/\d{4}\b");

    /// <summary>El comprobante leído por el primer layout que reconoce el texto, o null.</summary>
    public static ComprobanteLeido? Leer(IReadOnlyList<string> renglones, string? nombreArchivo = null)
        => Layouts.Select(leer => leer(renglones, nombreArchivo)).FirstOrDefault(c => c is not null);

    // ETMAN (Multioriginal Parts): factura; números en formato de EE. UU. (20,184.21); tres
    // descuentos con signo menos; la descripción sigue a veces en otro renglón, que no es línea.
    private const string EncabezadoEtman = "Marca Cantidad Precio Bonif. Bonif. Dto. Total";
    private static readonly Regex NumeroEtman = new(@"Nro\.: (\d{2}) (\d{4})- ?(\d{8})");
    private static readonly Regex LineaEtman = new(
        @"^(\S+) (.+) (\d+(?:\.\d+)?) ([\d,]+\.\d{2}) (-?\d+\.\d{2}) (-?\d+\.\d{2}) (-?\d+\.\d{2}) ([\d,]+\.\d{2})$");

    private static ComprobanteLeido? LeerEtman(IReadOnlyList<string> renglones, string? _)
    {
        var encabezado = Indice(renglones, r => r.Contains(EncabezadoEtman));
        if (encabezado < 0)
            return null;
        var texto = string.Join('\n', renglones);
        var revisar = new List<string>();

        var nro = NumeroEtman.Match(texto);
        // Códigos de comprobante de ARCA: 01 factura A, 06 factura B.
        var tipo = nro.Success ? nro.Groups[1].Value switch { "01" => "A", "06" => "B", _ => null } : null;
        var numero = nro.Success ? $"{nro.Groups[2].Value}-{nro.Groups[3].Value}" : null;
        // La emisión es la primera fecha después del rótulo «CLIENTE … FECHA».
        var cliente = Indice(renglones, r => r.Contains("CLIENTE") && r.Contains("FECHA"));
        var emision = cliente < 0 ? null : renglones.Skip(cliente + 1).Select(PrimeraFecha).FirstOrDefault(f => f is not null);
        // El vencimiento es el del neto; el del pago contado tiene descuento y no se usa.
        var vencimiento = renglones.Where(r => r.StartsWith("Vencimiento")).Select(PrimeraFecha).FirstOrDefault(f => f is not null);

        // D-S6-15: el descuento de pago contado («Pago Contado 5%») no viene en las líneas; depende
        // de la forma de pago, así que va como cuarto descuento propuesto y marcado para revisar.
        var pagoContado = Monto(texto, @"Pago Contado (\d+(?:\.\d+)?)%", Eeuu);
        var lineas = new List<LineaLeida>();
        foreach (var renglon in renglones.Skip(encabezado + 1))
        {
            var m = LineaEtman.Match(renglon);
            if (!m.Success)
                continue;
            var linea = new LineaLeida(m.Groups[1].Value, m.Groups[2].Value, Eeuu(m.Groups[3].Value), Eeuu(m.Groups[4].Value),
                Math.Abs(Eeuu(m.Groups[5].Value)), Math.Abs(Eeuu(m.Groups[6].Value)), Math.Abs(Eeuu(m.Groups[7].Value)), pagoContado ?? 0m);
            Agregar(lineas, linea, Eeuu(m.Groups[8].Value), afirmable: true, revisar);
            if (pagoContado is not null)
                revisar.Add($"lineas[{lineas.Count - 1}].descuento4");
        }

        // El bloque de totales de ETMAN siempre trae las dos percepciones: la que no se puede leer
        // se marca para revisar, nunca queda en null en silencio.
        var percepcionIva = Monto(texto, @"Percepcion de IVA ([\d,]+\.\d{2})(?!\S)", Eeuu);
        var percepcionIibb = Monto(texto, @"Percepcion IIBB: ([\d,]+\.\d{2})(?!\S)", Eeuu);
        Revisar(revisar, ("tipoComprobante", tipo), ("numero", numero), ("fechaEmision", emision),
            ("percepcionIva", percepcionIva), ("percepcionIibb", percepcionIibb));
        return new ComprobanteLeido(Cuits(texto), tipo, numero, emision, vencimiento,
            percepcionIva, percepcionIibb, null, null, lineas, revisar);
    }

    // Fazio (Badaloni): presupuesto, compra en negro; números en formato argentino (27.396,12);
    // el descuento viene combinado con sus partes sucesivas entre paréntesis: 43,84 (28+22), y
    // D-S6-17 (Santi, 18/11/2026): el costo es el «P. Final», lo que se paga, no el recálculo
    // desde el precio de lista, que puede diferir en un centavo por unidad. El
    // texto no trae el número del presupuesto ni el CUIT del emisor.
    private const string EncabezadoFazio = "Remito P. Lista % Desc. P. Final Total";
    private static readonly Regex LineaFazio = new(
        @"^\d+ (\S+) (.+) (\d+(?:,\d+)?) \S+ - [\d.]+,\d{2} [\d.]+,\d{2} \([\d,]+(?:\+[\d,]+)*\) ([\d.]+,\d{2}) ([\d.]+,\d{2})$");

    private static ComprobanteLeido? LeerFazio(IReadOnlyList<string> renglones, string? nombreArchivo)
    {
        var encabezado = Indice(renglones, r => r.Contains(EncabezadoFazio));
        if (encabezado < 0)
            return null;
        var texto = string.Join('\n', renglones);
        var revisar = new List<string>();
        var emision = Regex.Match(texto, @"Fecha : (\d{2}/\d{2}/\d{4})") is { Success: true } f ? AFecha(f.Groups[1].Value) : null;

        var lineas = new List<LineaLeida>();
        foreach (var renglon in renglones.Skip(encabezado + 1))
        {
            var m = LineaFazio.Match(renglon);
            if (!m.Success)
                continue;
            var linea = new LineaLeida(m.Groups[1].Value, m.Groups[2].Value, Argentino(m.Groups[3].Value), Argentino(m.Groups[4].Value),
                0m, 0m, 0m);
            Agregar(lineas, linea, Argentino(m.Groups[5].Value), afirmable: true, revisar);
        }

        // D-S6-13 (Sergio, 17/11/2026): lo comprado en negro es B, con 99 y el número del
        // presupuesto. El texto no lo trae; el nombre del archivo sí («Presupuesto_48437.pdf»).
        // Un nombre puede cambiarse a mano: el número queda siempre para revisar.
        var numero = Regex.Match(Path.GetFileName(nombreArchivo ?? ""), @"^presupuesto_(\d+)\.pdf$", RegexOptions.IgnoreCase)
            is { Success: true } n ? $"99-{n.Groups[1].Value}" : null;
        revisar.Add("numero");
        Revisar(revisar, ("fechaEmision", emision));
        return new ComprobanteLeido(Cuits(texto), "B", numero, emision, null, null, null, null, null, lineas, revisar);
    }

    /// <summary>
    /// Agrega la línea; si su total no cierra con cantidad, precio y descuentos sucesivos (o el
    /// lector ya sabe que no la puede afirmar), la marca para revisar.
    /// </summary>
    private static void Agregar(List<LineaLeida> lineas, LineaLeida linea, decimal total, bool afirmable, List<string> revisar)
    {
        var calculado = linea.Cantidad * linea.PrecioUnitario
            * (1 - linea.Descuento1 / 100) * (1 - linea.Descuento2 / 100) * (1 - linea.Descuento3 / 100);
        // El proveedor redondea el precio unitario a centavos antes de multiplicar. El cuarto
        // descuento no está en el comprobante (D-S6-15): no entra en el total del PDF.
        if (!afirmable || Math.Abs(calculado - total) > 0.01m + 0.005m * linea.Cantidad)
            revisar.Add($"lineas[{lineas.Count}]");
        lineas.Add(linea);
    }

    // Expoyer: factura; números en formato de EE. UU.; dos columnas de artículos por hoja, así
    // que un renglón puede traer dos; los precios ya vienen netos del descuento global de la
    // cabecera («Descuento: 61.08%+15»), sin descuentos por línea; códigos con espacios y
    // guiones internos («C 115105 4- F»). El CUIT del cliente viene sin guiones y no se toma.
    // Las tildes se ignoran: el texto real dice «Código», el sintético no puede escribirlas.
    private static readonly Regex EncabezadoExpoyer = new(@"Cant\. C.digo Descripci.n P\.Unit\. Total");
    private static readonly Regex ArticuloExpoyer = new(
        @"(\d+(?:\.\d+)?) (.+?) (\d{1,3}(?:,\d{3})*\.\d{2}) (\d{1,3}(?:,\d{3})*\.\d{2})");

    private static ComprobanteLeido? LeerExpoyer(IReadOnlyList<string> renglones, string? _)
    {
        var encabezado = Indice(renglones, r => EncabezadoExpoyer.IsMatch(r));
        if (encabezado < 0)
            return null;
        var texto = string.Join('\n', renglones);
        var revisar = new List<string>();

        // Código de comprobante de ARCA, sin el cero: 1 factura A, 6 factura B.
        var codigo = Regex.Match(texto, @"Comprobante\. C.digo N\S* (\d{1,2})\b");
        var tipo = codigo.Success ? codigo.Groups[1].Value switch { "1" => "A", "6" => "B", _ => null } : null;
        var numero = Regex.Match(texto, @"FACTURA N\S* (\d{4}-\d{8})") is { Success: true } n ? n.Groups[1].Value : null;
        var emision = Regex.Match(texto, @"FECHA: (\d{2}/\d{2}/\d{4})") is { Success: true } f ? AFecha(f.Groups[1].Value) : null;

        var lineas = new List<LineaLeida>();
        foreach (var renglon in renglones.Skip(encabezado + 1))
            foreach (Match m in ArticuloExpoyer.Matches(renglon))
            {
                var (codigoProveedor, descripcion) = CodigoYDescripcion(m.Groups[2].Value);
                var linea = new LineaLeida(codigoProveedor, descripcion, Eeuu(m.Groups[1].Value), Eeuu(m.Groups[3].Value), 0m, 0m, 0m);
                Agregar(lineas, linea, Eeuu(m.Groups[4].Value), afirmable: true, revisar);
            }

        Revisar(revisar, ("tipoComprobante", tipo), ("numero", numero), ("fechaEmision", emision));
        // shortcut: no hay una factura real de Expoyer con percepciones; si el texto las menciona,
        // las dos van a revisión en lugar de quedar en null en silencio. Con una real, leerlas.
        if (texto.Contains("percep", StringComparison.OrdinalIgnoreCase))
            revisar.AddRange(["percepcionIva", "percepcionIibb"]);
        // Sin fecha de vencimiento en el texto («CUENTA CORRIENTE 35 DIAS» no es una fecha): no se calcula.
        return new ComprobanteLeido(Cuits(texto), tipo, numero, emision, null, null, null, null, null, lineas, revisar);
    }

    // shortcut: el código es el primer token y los que le siguen mientras tengan un dígito o sean
    // una sola letra; la descripción empieza en la primera palabra que no. Un código sin
    // dígitos de dos letras o más se corta mal y la línea queda sin artículo (se resuelve a
    // mano). Si aparece, leer la columna por su posición en lugar del texto.
    private static (string Codigo, string Descripcion) CodigoYDescripcion(string texto)
    {
        var palabras = texto.Split(' ');
        var n = 1;
        while (n < palabras.Length - 1 && (palabras[n].Any(char.IsDigit) || palabras[n].Length == 1))
            n++;
        return (string.Join(' ', palabras[..n]), string.Join(' ', palabras[n..]));
    }

    private static void Revisar(List<string> revisar, params (string Campo, object? Valor)[] campos)
        => revisar.AddRange(campos.Where(c => c.Valor is null).Select(c => c.Campo));

    private static int Indice(IReadOnlyList<string> renglones, Func<string, bool> condicion)
        => renglones.Select((r, i) => (r, i)).FirstOrDefault(x => condicion(x.r), (r: "", i: -1)).i;

    private static string[] Cuits(string texto) => Cuit.Matches(texto).Select(m => m.Value).Distinct().ToArray();

    private static DateOnly? PrimeraFecha(string renglon) => Fecha.Match(renglon) is { Success: true } m ? AFecha(m.Value) : null;

    private static DateOnly? AFecha(string texto)
        => DateOnly.TryParseExact(texto, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var f) ? f : null;

    private static decimal? Monto(string texto, string patron, Func<string, decimal> formato)
        => Regex.Match(texto, patron) is { Success: true } m ? formato(m.Groups[1].Value) : null;

    private static decimal Eeuu(string numero) => decimal.Parse(numero.Replace(",", ""), CultureInfo.InvariantCulture);

    private static decimal Argentino(string numero)
        => decimal.Parse(numero.Replace(".", "").Replace(',', '.'), CultureInfo.InvariantCulture);
}

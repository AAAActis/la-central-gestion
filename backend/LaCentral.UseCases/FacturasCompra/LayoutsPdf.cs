using LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>
/// HU-FCO-05 (029): un lector por reglas para cada layout de proveedor conocido (decisión del
/// 17/11/2026, `docs/sprint06/ESTADO.md`, «Compuerta C7»). Cada uno reconoce su layout por el
/// encabezado de columnas y declara su formato de número. Un proveedor nuevo es un método más.
/// </summary>
public static class LayoutsPdf
{
    /// <summary>El comprobante leído por el primer layout que reconoce el texto, o null.</summary>
    public static ComprobanteLeido? Leer(IReadOnlyList<string> renglones) => null;
}

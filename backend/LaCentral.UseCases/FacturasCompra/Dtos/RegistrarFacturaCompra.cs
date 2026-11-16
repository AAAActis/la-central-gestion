namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>
/// Una línea del comprobante. D9: el destino de stock es de la línea. HU-FCO-06: hasta tres
/// descuentos, sucesivos (el segundo sobre el neto del primero).
/// </summary>
public sealed record LineaFacturaCompraRequest(
    int ArticuloId, short SucursalId, decimal Cantidad, decimal PrecioUnitario, decimal DescuentoPorcentaje,
    decimal Descuento2Porcentaje = 0m, decimal Descuento3Porcentaje = 0m);

/// <summary>
/// HU-FCO-01. Sin usuario: sale de la sesión. Las percepciones (D8) son datos del
/// comprobante; si no se informan, son cero, como el DEFAULT de la columna.
/// HU-FCO-06: el tipo de comprobante (A o B) es de esta factura; la imputación, si no se
/// informa, es la emisión.
/// </summary>
public sealed record RegistrarFacturaCompraRequest(
    int ProveedorId,
    string Numero,
    DateOnly FechaEmision,
    DateOnly? FechaVencimiento,
    decimal PercepcionIva,
    decimal PercepcionIibb,
    decimal PercepcionMunicipal,
    decimal PercepcionGanancias,
    IReadOnlyList<LineaFacturaCompraRequest>? Lineas,
    string? TipoComprobante = null,
    DateOnly? FechaImputacion = null);

public sealed record RegistrarFacturaCompraResponse(int Id);

/// <summary>La factura activa con la que choca un número repetido (CA2).</summary>
public sealed record FacturaCompraExistente(int Id, string Numero, DateOnly FechaEmision);

/// <summary>
/// HU-FCO-06 CA-014 y CA-015: lo que suma la factura antes de confirmarla. Neto: las líneas
/// con sus descuentos. IVA: 21 % del neto si el comprobante es A, cero si es B.
/// </summary>
public sealed record TotalesFacturaCompra(decimal Neto, decimal Iva, decimal Percepciones, decimal Total);

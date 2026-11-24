namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>
/// Una línea tal como la trae el comprobante. Los descuentos, en positivo y sucesivos. El
/// cuarto (D-S6-15) no viene en el comprobante: el layout lo propone y lo marca para revisar.
/// </summary>
public sealed record LineaLeida(
    string CodigoProveedor, string Descripcion, decimal Cantidad, decimal PrecioUnitario,
    decimal Descuento1, decimal Descuento2, decimal Descuento3, decimal Descuento4 = 0m);

/// <summary>
/// Lo que un layout reconoció del texto del PDF, sin tocar la base. <c>ParaRevisar</c> (sin orden que importe): los
/// campos que el layout no encontró o no puede afirmar con certeza (nombres del formulario).
/// <c>RazonSocial</c>: el nombre con el que el layout identifica a su proveedor, para buscarlo cuando el
/// CUIT no está o no viene. <c>Destinatario</c>: el texto del PDF donde figura a quién va la mercadería.
/// </summary>
public sealed record ComprobanteLeido(
    IReadOnlyList<string> Cuits,
    string? TipoComprobante,
    string? Numero,
    DateOnly? FechaEmision,
    DateOnly? FechaVencimiento,
    decimal? PercepcionIva,
    decimal? PercepcionIibb,
    decimal? PercepcionMunicipal,
    decimal? PercepcionGanancias,
    IReadOnlyList<LineaLeida> Lineas,
    IReadOnlyList<string> ParaRevisar,
    string? RazonSocial = null,
    string? Destinatario = null);

/// <summary>
/// HU-FCO-05 CA-004: una línea sin artículo (<c>ArticuloId</c> null) queda no resuelta; el
/// sistema no crea artículos.
/// </summary>
public sealed record LineaPdfInterpretada(
    string CodigoProveedor, string Descripcion, int? ArticuloId, decimal Cantidad, decimal PrecioUnitario,
    decimal DescuentoPorcentaje, decimal Descuento2Porcentaje, decimal Descuento3Porcentaje, decimal Descuento4Porcentaje = 0m);

/// <summary>
/// HU-FCO-05 CA-001 y CA-003: la propuesta para el formulario editable. No se guardó nada.
/// <c>ParaRevisar</c> nombra los campos que el operador tiene que mirar (049 CA-002).
/// </summary>
public sealed record FacturaPdfInterpretada(
    int? ProveedorId,
    string? TipoComprobante,
    string? Numero,
    DateOnly? FechaEmision,
    DateOnly? FechaVencimiento,
    decimal? PercepcionIva,
    decimal? PercepcionIibb,
    decimal? PercepcionMunicipal,
    decimal? PercepcionGanancias,
    IReadOnlyList<LineaPdfInterpretada> Lineas,
    IReadOnlyList<string> ParaRevisar,
    short? SucursalSugeridaId = null);

/// <summary>
/// HU-FCO-07 (057) CA-010: lo que un lector sacó del texto de una nota de crédito de proveedor.
/// Siempre A (regla d): no hay tipo. Las facturas asociadas son números («0011-00044774»), sin
/// resolver todavía. ParaRevisar nombra lo que no se pudo afirmar.
/// </summary>
public sealed record NotaCreditoLeida(
    IReadOnlyList<string> Cuits, string? Numero, DateOnly? FechaEmision, string? Clase, decimal? ImporteNeto,
    IReadOnlyList<string> FacturasAsociadas, IReadOnlyList<string> ParaRevisar);

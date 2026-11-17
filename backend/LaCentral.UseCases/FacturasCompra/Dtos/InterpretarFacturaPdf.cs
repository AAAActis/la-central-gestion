namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>Una línea tal como la trae el comprobante. Los descuentos, en positivo y sucesivos.</summary>
public sealed record LineaLeida(
    string CodigoProveedor, string Descripcion, decimal Cantidad, decimal PrecioUnitario,
    decimal Descuento1, decimal Descuento2, decimal Descuento3);

/// <summary>
/// Lo que un layout reconoció del texto del PDF, sin tocar la base. <c>ParaRevisar</c>: los
/// campos que el layout no encontró o no puede afirmar con certeza (nombres del formulario).
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
    IReadOnlyList<string> ParaRevisar);

/// <summary>
/// HU-FCO-05 CA-004: una línea sin artículo (<c>ArticuloId</c> null) queda no resuelta; el
/// sistema no crea artículos.
/// </summary>
public sealed record LineaPdfInterpretada(
    string CodigoProveedor, string Descripcion, int? ArticuloId, decimal Cantidad, decimal PrecioUnitario,
    decimal DescuentoPorcentaje, decimal Descuento2Porcentaje, decimal Descuento3Porcentaje);

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
    IReadOnlyList<string> ParaRevisar);

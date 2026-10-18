namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>Una línea del comprobante. D9: el destino de stock es de la línea.</summary>
public sealed record LineaFacturaCompraRequest(
    int ArticuloId, short SucursalId, decimal Cantidad, decimal PrecioUnitario, decimal DescuentoPorcentaje);

/// <summary>
/// HU-FCO-01. Sin usuario: sale de la sesión. Las percepciones (D8) son datos del
/// comprobante; si no se informan, son cero, como el DEFAULT de la columna.
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
    IReadOnlyList<LineaFacturaCompraRequest>? Lineas);

public sealed record RegistrarFacturaCompraResponse(int Id);

/// <summary>La factura activa con la que choca un número repetido (CA2).</summary>
public sealed record FacturaCompraExistente(int Id, string Numero, DateOnly FechaEmision);

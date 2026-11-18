namespace LaCentral.UseCases.NotasCreditoCompra.Dtos;

/// <summary>Un artículo devuelto: sale del stock de SucursalId (una sucursal o Reclamos).</summary>
public sealed record LineaNotaCreditoCompraRequest(int ArticuloId, short SucursalId, decimal Cantidad);

/// <summary>
/// HU-FCO-07 (057). Sin tipo de comprobante: es siempre A. Sin usuario: sale de la sesión.
/// Clase: BONIFICACION (sin líneas) o DEVOLUCION (con líneas). Las percepciones (D8) son datos
/// del comprobante.
/// </summary>
public sealed record RegistrarNotaCreditoCompraRequest(
    int ProveedorId,
    string Numero,
    DateOnly FechaEmision,
    string? Clase,
    decimal ImporteNeto,
    decimal? PercepcionIva,
    decimal? PercepcionIibb,
    decimal? PercepcionMunicipal,
    decimal? PercepcionGanancias,
    IReadOnlyList<int>? FacturaIds,
    IReadOnlyList<LineaNotaCreditoCompraRequest>? Lineas,
    bool DesdePdf = false);

public sealed record RegistrarNotaCreditoCompraResponse(int Id);

/// <summary>CA-007: neto, 21 % de IVA (siempre A), percepciones y total.</summary>
public sealed record TotalesNotaCreditoCompra(decimal Neto, decimal Iva, decimal Percepciones, decimal Total);

/// <summary>Lo que hace falta saber de una factura para asociarle una nota de crédito.</summary>
public sealed record FacturaParaNotaCredito(int Id, int ProveedorId, bool Activa);

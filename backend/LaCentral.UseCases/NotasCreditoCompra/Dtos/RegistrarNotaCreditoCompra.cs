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

/// <summary>
/// HU-FCO-07 (057) CA-010: la nota de crédito propuesta desde el PDF, para revisar antes de
/// registrarla por la carga manual. FacturasSinEncontrar: los números de «Doc.Asoc.» que no son
/// una factura activa del proveedor (la nota no se registra sin al menos una factura).
/// </summary>
public sealed record NotaCreditoPdfInterpretada(
    int? ProveedorId, string? Numero, DateOnly? FechaEmision, string? Clase, decimal? ImporteNeto,
    IReadOnlyList<int> FacturaIds, IReadOnlyList<string> FacturasSinEncontrar, IReadOnlyList<string> ParaRevisar);

/// <summary>CA-014: de un artículo, lo comprado en las facturas referenciadas y lo devuelto en notas vigentes.</summary>
public sealed record CompradoYDevuelto(int ArticuloId, decimal Comprado, decimal Devuelto);

/// <summary>CA-012 (D-S6-23): pedido de anulación, como el de la factura (028).</summary>
public sealed record AnularNotaCreditoCompraRequest(string? Motivo, string? UsuarioAutoriza, string? ContrasenaAutoriza);

public sealed record NotaCreditoParaAnular(int Id, string Numero, bool Activa);

/// <summary>CA-012: lo que se escribe al anular. Si la nota era por devolución, el stock vuelve a cada origen.</summary>
public sealed record AnulacionNotaCreditoCompra(int NotaId, string Motivo, int UsuarioId, int UsuarioAutorizacionId, DateTime Fecha);

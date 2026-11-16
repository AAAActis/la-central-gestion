namespace LaCentral.UseCases.FacturasCompra.Dtos;

/// <summary>HU-FCO-02 (026): todos los criterios son opcionales y se combinan.</summary>
public sealed record FiltroFacturasCompra(
    int? ProveedorId = null,
    string? Numero = null,
    DateOnly? EmisionDesde = null,
    DateOnly? EmisionHasta = null,
    DateOnly? VencimientoDesde = null,
    DateOnly? VencimientoHasta = null);

/// <summary>Una fila del listado. CA-005: las anuladas vienen marcadas.</summary>
public sealed record FacturaCompraResumen(
    int Id, int ProveedorId, string Proveedor, string Numero, string TipoComprobante,
    DateOnly FechaEmision, DateOnly? FechaVencimiento, bool Anulada);

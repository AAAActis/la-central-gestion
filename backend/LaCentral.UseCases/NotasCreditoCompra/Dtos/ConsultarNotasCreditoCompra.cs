namespace LaCentral.UseCases.NotasCreditoCompra.Dtos;

/// <summary>HU-FCO-07 (057) CA-012: el listado de notas, por proveedor y rango de emisión; todo opcional.</summary>
public sealed record FiltroNotasCreditoCompra(int? ProveedorId = null, DateOnly? EmisionDesde = null, DateOnly? EmisionHasta = null);

/// <summary>Una fila del listado. Las anuladas vienen marcadas, como en la 026.</summary>
public sealed record NotaCreditoCompraResumen(
    int Id, int ProveedorId, string Proveedor, string Numero, string Clase, DateOnly FechaEmision, decimal ImporteNeto, bool Anulada);

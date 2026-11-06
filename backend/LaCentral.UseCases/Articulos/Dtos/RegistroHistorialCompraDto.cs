namespace LaCentral.UseCases.Articulos.Dtos;

/// <summary>016 · un registro del historial de compras: lo escribe la factura de compra (HU-FCO-01), nunca a mano.</summary>
public sealed record RegistroHistorialCompraDto(
    DateTime Fecha, int ProveedorId, string Proveedor, decimal PrecioCosto,
    int FacturaCompraId, string NumeroFactura, bool Anulado);

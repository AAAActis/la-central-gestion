namespace LaCentral.UseCases.Proveedores.Dtos;

/// <summary>045 · CA-002 y CA-003: la ficha del proveedor, con todos sus teléfonos y direcciones.</summary>
public record ProveedorDetalleDto(
    int Id,
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string? UrlReferencia,
    bool Activo,
    string? MotivoBaja,
    IReadOnlyList<string> Telefonos,
    IReadOnlyList<string> Direcciones);

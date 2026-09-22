namespace LaCentral.UseCases.Proveedores.Dtos;

/// <summary>Fila de resultado de HU-PRO-02: lo mínimo para identificar un proveedor en una búsqueda.</summary>
public record ProveedorResumenDto(
    int Id,
    string RazonSocial,
    string? Cuit,
    string? UrlReferencia,
    bool Activo
);

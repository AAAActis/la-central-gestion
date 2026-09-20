namespace LaCentral.UseCases.Proveedores.Dtos;

public record CrearProveedorResponse(
    string Cuit,
    string? Advertencia
);
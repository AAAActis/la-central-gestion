namespace LaCentral.UseCases.Proveedores.Dtos;

public record CrearProveedorRequest(
    string RazonSocial,
    string Cuit,
    string? UrlReferencia,
    List<string>? Telefonos,
    List<string>? Direcciones
);
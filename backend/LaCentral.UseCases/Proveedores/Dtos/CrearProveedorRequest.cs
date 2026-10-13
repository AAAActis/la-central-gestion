namespace LaCentral.UseCases.Proveedores.Dtos;

public record CrearProveedorRequest(
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string? UrlReferencia,
    List<string>? Telefonos,
    List<string>? Direcciones
);
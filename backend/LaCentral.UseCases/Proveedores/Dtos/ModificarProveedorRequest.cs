namespace LaCentral.UseCases.Proveedores.Dtos;

public record ModificarProveedorRequest(
    string RazonSocial,
    string Cuit,
    string? UrlReferencia,
    List<string>? Telefonos,
    List<string>? Direcciones
);
namespace LaCentral.UseCases.Clientes.Dtos;

public record ModificarClienteRequest(
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string CondicionFiscal,
    string CondicionPago,
    List<string>? Telefonos,
    List<string>? Direcciones
);
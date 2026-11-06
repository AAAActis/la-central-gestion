namespace LaCentral.UseCases.Clientes.Dtos;

public record ModificarClienteRequest(
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string? CondicionFiscal, // 045: vacía o ausente es válida
    string CondicionPago,
    List<string>? Telefonos,
    List<string>? Direcciones
);
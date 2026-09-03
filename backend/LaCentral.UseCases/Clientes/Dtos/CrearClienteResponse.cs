namespace LaCentral.UseCases.Clientes.Dtos;

public record CrearClienteResponse(
    string Codigo,
    string? Advertencia // CA-002: Acá devolvemos la advertencia si la razón social se repite
);
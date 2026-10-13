namespace LaCentral.UseCases.Clientes.Dtos;

public record CrearClienteRequest(
    string Codigo,
    string RazonSocial,
    string? Cuit, // Ahora es opcional por la regla de negocio nueva
    string CondicionFiscal,
    string CondicionPago,
    List<string> Telefonos,   // Asumiendo que mandan strings simples por ahora
    List<string> Direcciones 
);
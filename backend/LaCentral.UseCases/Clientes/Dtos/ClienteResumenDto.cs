namespace LaCentral.UseCases.Clientes.Dtos;

/// <summary>Fila de resultado de HU-CLI-02: lo mínimo para identificar un cliente en una lista de búsqueda.</summary>
public record ClienteResumenDto(
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string CondicionFiscal,
    string CondicionPago,
    bool Activo
);

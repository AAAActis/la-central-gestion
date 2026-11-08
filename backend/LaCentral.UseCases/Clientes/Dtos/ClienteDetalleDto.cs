namespace LaCentral.UseCases.Clientes.Dtos;

/// <summary>
/// Representa el detalle completo de un cliente (HU-CLI-02 / CA-005), 
/// incluyendo sus colecciones 1:N.
/// </summary>
public record ClienteDetalleDto(
    string Codigo,
    string RazonSocial,
    string? Cuit,
    string CondicionFiscal,
    string CondicionPago,
    bool Activo,
    // D-S5-08: el de la ÚLTIMA baja. Reactivado, queda como rastro (D10).
    string? MotivoBaja,
    IReadOnlyList<string> Telefonos,
    IReadOnlyList<string> Direcciones
);
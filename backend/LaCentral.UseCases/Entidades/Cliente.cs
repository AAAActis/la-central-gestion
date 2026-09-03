namespace LaCentral.UseCases.Entidades;

public class Cliente
{
    // Propiedades principales
    public string Codigo { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Cuit { get; set; } // Opcional, como pide la regla de negocio

    // CA-004: Listas para teléfonos y direcciones (Santi después mapeará esto a sus tablas con EF Core)
    public List<string> Telefonos { get; set; } = new();
    public List<string> Direcciones { get; set; } = new();
}
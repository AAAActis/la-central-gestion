namespace LaCentral.UseCases.Entidades;

public class Proveedor
{
    public int Id { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string? UrlReferencia { get; set; }
    public bool Activo { get; set; }
    
    // Colecciones 1:N mapeadas como primitivos para aislar el dominio
    public List<string> Telefonos { get; set; } = new();
    public List<string> Direcciones { get; set; } = new();
}
namespace LaCentral.UseCases.Entidades;

public class Proveedor
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string? UrlReferencia { get; set; }
    public bool Activo { get; set; }
    
    public List<string> Telefonos { get; set; } = new();
    public List<string> Direcciones { get; set; } = new();
    public string? MotivoBaja { get; set; }
}
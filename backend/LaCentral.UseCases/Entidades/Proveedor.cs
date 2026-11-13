namespace LaCentral.UseCases.Entidades;

public class Proveedor
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? Cuit { get; set; }
    public string? UrlReferencia { get; set; }
    /// <summary>HU-FCO-06 CA-004: "A", "B" o null. Solo el valor por defecto de la cabecera.</summary>
    public string? TipoComprobanteHabitual { get; set; }
    public bool Activo { get; set; } = true;
    
    public List<string> Telefonos { get; set; } = new();
    public List<string> Direcciones { get; set; } = new();
    public string? MotivoBaja { get; set; }
    public DateTime? FechaBaja { get; set; }
}
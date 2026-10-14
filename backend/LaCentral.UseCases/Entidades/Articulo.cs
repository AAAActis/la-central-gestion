namespace LaCentral.UseCases.Entidades;

public class CodigoAlternativo
{
    public string Codigo { get; set; }
    public int ProveedorId { get; set; }

    public CodigoAlternativo(string codigo, int proveedorId)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código no puede estar vacío");
        if (proveedorId <= 0) throw new ArgumentException("El código debe pertenecer a un proveedor válido");
        
        Codigo = codigo;
        ProveedorId = proveedorId;
    }
}

// 1. D1: Existencias dinámicas identificadas por SucursalId
public class Existencia
{
    public int SucursalId { get; set; }
    public decimal Cantidad { get; set; }

    public Existencia(int sucursalId, decimal cantidad)
    {
        if (sucursalId <= 0) throw new ArgumentException("Sucursal inválida");
        SucursalId = sucursalId;
        Cantidad = cantidad;
    }
}

public class Articulo
{
    public int Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }

    // El CHECK articulo_baja_coherente exige motivo Y fecha cuando activo = false;
    // una fila activa puede conservarlos como rastro de su última baja (D10).
    public string? MotivoBaja { get; set; }
    public DateTime? FechaBaja { get; set; }
    
    public decimal? CostoVigente { get; set; }

    public List<CodigoAlternativo> CodigosAlternativos { get; set; } = new();
    
    public List<Existencia> Existencias { get; set; } = new();
    
    // D1: las seis filas de articulo_precio_modalidad. La fórmula vive en
    // ReglasPrecioModalidad, no en la entidad.
    public List<PrecioModalidad> Precios { get; set; } = new();
}

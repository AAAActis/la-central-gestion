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

// 2. D1: Márgenes persistidos por modalidad
public class MargenModalidad
{
    public int ModalidadId { get; set; }
    public decimal Porcentaje { get; set; }

    public MargenModalidad(int modalidadId, decimal porcentaje)
    {
        ModalidadId = modalidadId;
        Porcentaje = porcentaje;
    }
}

public class Articulo
{
    public int Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    
    public decimal? CostoVigente { get; set; }

    public List<CodigoAlternativo> CodigosAlternativos { get; set; } = new();
    
    public List<Existencia> Existencias { get; set; } = new();
    
    public List<MargenModalidad> Margenes { get; set; } = new();

    // Contrato base para que el equipo avance. La lógica pesada de recálculo queda para ART-05.
    public decimal? ObtenerPrecioEstimado(int modalidadId)
    {
        if (CostoVigente is null) return null;

        var margen = Margenes.FirstOrDefault(m => m.ModalidadId == modalidadId);
        if (margen is null) return null;

        // D1: Costo al 0% devuelve el costo neto
        return CostoVigente.Value * (1 + (margen.Porcentaje / 100m));
    }
}
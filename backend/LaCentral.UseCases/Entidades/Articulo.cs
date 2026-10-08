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

    public decimal? CalcularPrecioEstimado(int modalidadId)
    {
        // TODO 1: Sin precio de costo -> null
        if (CostoVigente is null)
        {
            return null;
        }

        var margen = Margenes.FirstOrDefault(m => m.ModalidadId == modalidadId);
        
        // TODO 2: Sin márgenes cargados para la modalidad -> null
        if (margen is null)
        {
            return null;
        }

        // TODO 3: Fórmula según D1
        decimal factor = 1m + (margen.Porcentaje / 100m);
        decimal precioCalculado = CostoVigente.Value * factor;

        // TODO 4: Redondeo explícito a 2 decimales, forzando la mitad hacia arriba
        return Math.Round(precioCalculado, 2, MidpointRounding.AwayFromZero);
    }
}
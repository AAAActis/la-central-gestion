namespace LaCentral.UseCases.Entidades;

public class CodigoAlternativo
{
    public string Codigo { get; set; }
    public int ProveedorId { get; set; }

    // Constructor para forzar CA-005 (no hay código sin proveedor)
    public CodigoAlternativo(string codigo, int proveedorId)
    {
        if (string.IsNullOrWhiteSpace(codigo)) throw new ArgumentException("El código no puede estar vacío");
        if (proveedorId <= 0) throw new ArgumentException("El código debe pertenecer a un proveedor válido");
        
        Codigo = codigo;
        ProveedorId = proveedorId;
    }
}

public class Articulo
{
    // Hallazgo 3: El Id va al dominio
    public int Id { get; set; }
    public string CodigoInterno { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    
    // Márgenes (HU-ART-05 y tabla articulo_margen)
    public decimal? MargenGanancia { get; set; }
    public decimal CostoVigente { get; set; }

    // Ubicaciones (CA-001 de ART-01: existencia cero en ambas ubicaciones)
    public int StockDeposito { get; set; }
    public int StockMostrador { get; set; }

    // Códigos alternativos como parte del agregado (tabla articulo_codigo_alternativo)
    public List<CodigoAlternativo> CodigosAlternativos { get; set; } = new();

    // Constantes de negocio (Percepciones para el cálculo de precio estimado)
    private const decimal Percepcion1 = 0.50m;
    private const decimal Percepcion2 = 0.40m;

    public decimal? PrecioVentaEstimado
    {
        get
        {
            if (MargenGanancia is null) return null;
            decimal costoConPercepciones = CostoVigente * (1 + Percepcion1) * (1 + Percepcion2);
            if (MargenGanancia == 0.01m) return costoConPercepciones; // Venta al costo
            return costoConPercepciones * (1 + (MargenGanancia.Value / 100));
        }
    }
}
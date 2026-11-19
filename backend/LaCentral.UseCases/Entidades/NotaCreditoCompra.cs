namespace LaCentral.UseCases.Entidades;

/// <summary>
/// HU-FCO-07 (057) · Nota de crédito de un proveedor, en pesos (D7). Siempre tipo A: el número
/// se copia exacto y el IVA es el 21 % del neto. No cambia el costo de ningún artículo: es plata
/// a favor con el proveedor, cuya cuenta corriente el sistema no lleva (Sergio, 18/11/2026).
/// </summary>
public class NotaCreditoCompra
{
    public const string Bonificacion = "BONIFICACION";
    public const string Devolucion = "DEVOLUCION";

    public int ProveedorId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly FechaEmision { get; set; }
    /// <summary><see cref="Bonificacion"/> (solo importes) o <see cref="Devolucion"/> (mueve stock).</summary>
    public string Clase { get; set; } = string.Empty;
    public decimal ImporteNeto { get; set; }
    public decimal? PercepcionIva { get; set; }
    public decimal? PercepcionIibb { get; set; }
    public decimal? PercepcionMunicipal { get; set; }
    public decimal? PercepcionGanancias { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool DesdePdf { get; set; }
    /// <summary>Las facturas de compra del mismo proveedor a las que corresponde: al menos una.</summary>
    public List<int> FacturaIds { get; set; } = new();
    /// <summary>Solo en una devolución: lo que sale del stock de cada origen.</summary>
    public List<LineaNotaCreditoCompra> Lineas { get; set; } = new();
}

/// <summary>Un artículo devuelto y su origen (una sucursal o Reclamos), elegido por línea (D9).</summary>
public class LineaNotaCreditoCompra
{
    public int ArticuloId { get; set; }
    public short SucursalId { get; set; }
    public decimal Cantidad { get; set; }
}

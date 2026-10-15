namespace LaCentral.UseCases.Entidades;

/// <summary>
/// HU-FCO-01 · Factura de compra de un proveedor, en pesos (D7). Las percepciones (D8),
/// la emisión y el vencimiento son datos transcriptos del comprobante: el sistema no
/// liquida impuestos ni calcula nada con ellos.
/// </summary>
public class FacturaCompra
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly FechaEmision { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public decimal PercepcionIva { get; set; }
    public decimal PercepcionIibb { get; set; }
    public decimal PercepcionMunicipal { get; set; }
    public decimal PercepcionGanancias { get; set; }
    public int UsuarioId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public List<FacturaCompraDetalle> Lineas { get; set; } = new();
}

/// <summary>Una línea: un artículo, su destino de stock (D9) y lo que costó.</summary>
public class FacturaCompraDetalle
{
    public int ArticuloId { get; set; }
    public short SucursalId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }

    /// <summary>
    /// El costo unitario que pasa a ser el costo vigente del artículo y queda en su
    /// historial de compra: el precio neto del descuento de la línea, en centavos como
    /// articulo.precio_costo (NUMERIC(14,2)). El medio centavo se aleja del cero, como
    /// redondea PostgreSQL. Las percepciones son de la cabecera y no entran (D8).
    /// </summary>
    public decimal CostoUnitario =>
        Math.Round(PrecioUnitario * (100m - DescuentoPorcentaje) / 100m, 2, MidpointRounding.AwayFromZero);
}

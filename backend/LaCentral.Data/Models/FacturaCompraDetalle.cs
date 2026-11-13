using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class FacturaCompraDetalle
{
    public int FacturaCompraId { get; set; }

    public int ArticuloId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    /// <summary>
    /// Destino de la mercadería de ESTA línea. Una factura puede repartirse entre Fragueiro y San Vicente, y el sistema anterior lo carga por línea (APS-005, columna «Dep.»). El stock se incrementa contra esta sucursal, no contra una del comprobante.
    /// </summary>
    public short SucursalId { get; set; }

    /// <summary>
    /// Segundo descuento de la línea, sucesivo: se aplica sobre el neto del primero (HU-FCO-06; supuesto a confirmar con Sergio).
    /// </summary>
    public decimal Descuento2Porcentaje { get; set; }

    /// <summary>
    /// Tercer descuento de la línea, sucesivo: se aplica sobre el neto del segundo.
    /// </summary>
    public decimal Descuento3Porcentaje { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual FacturaCompra FacturaCompra { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}

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

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual FacturaCompra FacturaCompra { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}

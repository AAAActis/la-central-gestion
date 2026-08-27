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

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual FacturaCompra FacturaCompra { get; set; } = null!;
}

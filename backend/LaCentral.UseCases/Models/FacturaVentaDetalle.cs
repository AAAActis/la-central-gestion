using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class FacturaVentaDetalle
{
    public int FacturaVentaId { get; set; }

    public int ArticuloId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual FacturaVentum FacturaVenta { get; set; } = null!;
}

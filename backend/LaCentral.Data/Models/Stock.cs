using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Existencia por artículo y ubicación. Admite valores negativos: la venta sin stock no se bloquea (HU-STK-03).
/// </summary>
public partial class Stock
{
    public int ArticuloId { get; set; }

    public short SucursalId { get; set; }

    public decimal Cantidad { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}

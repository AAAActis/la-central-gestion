using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ArticuloHistorialCompra
{
    public int Id { get; set; }

    public int ArticuloId { get; set; }

    public int ProveedorId { get; set; }

    public int FacturaCompraId { get; set; }

    public decimal PrecioCosto { get; set; }

    public DateTime Fecha { get; set; }

    public bool Anulado { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual FacturaCompra FacturaCompra { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Facturas de compra a las que corresponde una nota de crédito: muchos a muchos. Las dos FK compuestas con proveedor_id hacen que la base rechace una factura de otro proveedor (Sergio, 18/11/2026).
/// </summary>
public partial class NotaCreditoCompraFactura
{
    public int NotaCreditoCompraId { get; set; }

    public int FacturaCompraId { get; set; }

    public int ProveedorId { get; set; }

    public virtual FacturaCompra FacturaCompra { get; set; } = null!;

    public virtual NotaCreditoCompra NotaCreditoCompra { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Mercadería devuelta en una nota de crédito por devolución. Sin precio: la nota no cambia costos.
/// </summary>
public partial class NotaCreditoCompraDetalle
{
    public int NotaCreditoCompraId { get; set; }

    public int ArticuloId { get; set; }

    /// <summary>
    /// Origen de la mercadería de ESTA línea: la sucursal que la devuelve o el depósito Reclamos, elegido por línea como el destino de la factura (D9). El stock baja contra esta ubicación.
    /// </summary>
    public short SucursalId { get; set; }

    public decimal Cantidad { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual NotaCreditoCompra NotaCreditoCompra { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}

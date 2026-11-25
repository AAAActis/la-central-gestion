using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Sucursal
{
    public short Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    /// <summary>
    /// TRUE solo para el depósito Reclamos, donde el negocio sigue la mercadería en reclamo al proveedor. Es una ubicación más (stock, transferencias, origen de una devolución), pero no es destino de una factura de compra (HU-FCO-07, D-S6-20, 18/11/2026).
    /// </summary>
    public bool EsReclamos { get; set; }

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalle { get; set; } = new List<FacturaCompraDetalle>();

    public virtual ICollection<FacturaVenta> FacturaVenta { get; set; } = new List<FacturaVenta>();

    public virtual ICollection<NotaCreditoCompraDetalle> NotaCreditoCompraDetalle { get; set; } = new List<NotaCreditoCompraDetalle>();

    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();

    public virtual ICollection<Transferencia> TransferenciaSucursalDestino { get; set; } = new List<Transferencia>();

    public virtual ICollection<Transferencia> TransferenciaSucursalOrigen { get; set; } = new List<Transferencia>();

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}

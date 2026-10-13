using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Sucursal
{
    public short Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalle { get; set; } = new List<FacturaCompraDetalle>();

    public virtual ICollection<FacturaVenta> FacturaVenta { get; set; } = new List<FacturaVenta>();

    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();

    public virtual ICollection<Transferencia> TransferenciaSucursalDestino { get; set; } = new List<Transferencia>();

    public virtual ICollection<Transferencia> TransferenciaSucursalOrigen { get; set; } = new List<Transferencia>();

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}

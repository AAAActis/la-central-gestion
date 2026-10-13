using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Transferencia
{
    public int Id { get; set; }

    public short SucursalOrigenId { get; set; }

    public short SucursalDestinoId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime Fecha { get; set; }

    public string? Observaciones { get; set; }

    public virtual Sucursal SucursalDestino { get; set; } = null!;

    public virtual Sucursal SucursalOrigen { get; set; } = null!;

    public virtual ICollection<TransferenciaDetalle> TransferenciaDetalle { get; set; } = new List<TransferenciaDetalle>();

    public virtual Usuario Usuario { get; set; } = null!;
}

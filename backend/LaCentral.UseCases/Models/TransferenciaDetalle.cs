using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class TransferenciaDetalle
{
    public int TransferenciaId { get; set; }

    public int ArticuloId { get; set; }

    public decimal Cantidad { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Transferencium Transferencia { get; set; } = null!;
}

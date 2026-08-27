using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class PrecioProveedor
{
    public int ArticuloId { get; set; }

    public int ProveedorId { get; set; }

    public decimal? PrecioLista { get; set; }

    public decimal? PrecioBonificado { get; set; }

    public DateTime FechaObtencion { get; set; }

    public string Origen { get; set; } = null!;

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}

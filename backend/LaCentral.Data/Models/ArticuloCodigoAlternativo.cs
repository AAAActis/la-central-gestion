using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ArticuloCodigoAlternativo
{
    public int Id { get; set; }

    public int ArticuloId { get; set; }

    public int ProveedorId { get; set; }

    public string Codigo { get; set; } = null!;

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Proveedor Proveedor { get; set; } = null!;
}

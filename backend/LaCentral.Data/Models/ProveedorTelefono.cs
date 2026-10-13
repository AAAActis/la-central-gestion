using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ProveedorTelefono
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Numero { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;
}

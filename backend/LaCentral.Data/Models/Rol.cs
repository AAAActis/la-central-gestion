using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Rol
{
    public short Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Usuario> Usuario { get; set; } = new List<Usuario>();
}

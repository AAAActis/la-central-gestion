using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ClienteTelefono
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string Numero { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;
}

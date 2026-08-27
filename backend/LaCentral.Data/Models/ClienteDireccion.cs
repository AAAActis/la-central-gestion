using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ClienteDireccion
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string? Calle { get; set; }

    public string? Barrio { get; set; }

    public string? Localidad { get; set; }

    public string? Provincia { get; set; }

    public string? CodigoPostal { get; set; }

    public string? Descripcion { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;
}

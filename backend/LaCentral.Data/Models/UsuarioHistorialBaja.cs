using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class UsuarioHistorialBaja
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaBaja { get; set; }

    public DateTime? FechaReactivacion { get; set; }

    public int? UsuarioBajaId { get; set; }

    public int? UsuarioReactivacionId { get; set; }

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioBaja { get; set; }

    public virtual Usuario? UsuarioReactivacion { get; set; }
}

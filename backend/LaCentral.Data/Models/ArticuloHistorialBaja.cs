using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ArticuloHistorialBaja
{
    public int Id { get; set; }

    public int ArticuloId { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaBaja { get; set; }

    public DateTime? FechaReactivacion { get; set; }

    public int? UsuarioBajaId { get; set; }

    public int? UsuarioReactivacionId { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Usuario? UsuarioBaja { get; set; }

    public virtual Usuario? UsuarioReactivacion { get; set; }
}

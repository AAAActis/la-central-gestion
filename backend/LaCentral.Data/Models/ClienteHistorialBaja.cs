using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Una fila por cada baja. fecha_reactivacion NULL = baja vigente. El índice único parcial impide dos bajas abiertas simultáneas del mismo cliente.
/// </summary>
public partial class ClienteHistorialBaja
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaBaja { get; set; }

    public DateTime? FechaReactivacion { get; set; }

    public int? UsuarioBajaId { get; set; }

    public int? UsuarioReactivacionId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Usuario? UsuarioBaja { get; set; }

    public virtual Usuario? UsuarioReactivacion { get; set; }
}

using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Un usuario representa un puesto de trabajo, no una persona. La trazabilidad alcanza a la terminal desde la que se operó.
/// </summary>
public partial class Usuario
{
    public int Id { get; set; }

    public string NombreUsuario { get; set; } = null!;

    public string HashContrasena { get; set; } = null!;

    public short RolId { get; set; }

    public short SucursalId { get; set; }

    public bool Activo { get; set; }

    public string? MotivoBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaAlta { get; set; }

    public virtual ICollection<FacturaCompra> FacturaCompraUsuarioAnulacions { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<FacturaCompra> FacturaCompraUsuarios { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<FacturaVentum> FacturaVentumUsuarioAnulacions { get; set; } = new List<FacturaVentum>();

    public virtual ICollection<FacturaVentum> FacturaVentumUsuarios { get; set; } = new List<FacturaVentum>();

    public virtual Rol Rol { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual ICollection<Transferencium> Transferencia { get; set; } = new List<Transferencium>();
}

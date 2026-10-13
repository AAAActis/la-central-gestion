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

    public int? UsuarioBajaId { get; set; }

    public int? UsuarioAltaId { get; set; }

    public virtual ICollection<ArticuloHistorialBaja> ArticuloHistorialBajaUsuarioBaja { get; set; } = new List<ArticuloHistorialBaja>();

    public virtual ICollection<ArticuloHistorialBaja> ArticuloHistorialBajaUsuarioReactivacion { get; set; } = new List<ArticuloHistorialBaja>();

    public virtual ICollection<ClienteHistorialBaja> ClienteHistorialBajaUsuarioBaja { get; set; } = new List<ClienteHistorialBaja>();

    public virtual ICollection<ClienteHistorialBaja> ClienteHistorialBajaUsuarioReactivacion { get; set; } = new List<ClienteHistorialBaja>();

    public virtual ICollection<Cliente> ClienteUsuarioAlta { get; set; } = new List<Cliente>();

    public virtual ICollection<Cliente> ClienteUsuarioBaja { get; set; } = new List<Cliente>();

    public virtual ICollection<FacturaCompra> FacturaCompraUsuario { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<FacturaCompra> FacturaCompraUsuarioAnulacion { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<FacturaVenta> FacturaVentaUsuario { get; set; } = new List<FacturaVenta>();

    public virtual ICollection<FacturaVenta> FacturaVentaUsuarioAnulacion { get; set; } = new List<FacturaVenta>();

    public virtual ICollection<Usuario> InverseUsuarioAlta { get; set; } = new List<Usuario>();

    public virtual ICollection<Usuario> InverseUsuarioBaja { get; set; } = new List<Usuario>();

    public virtual ICollection<ProveedorHistorialBaja> ProveedorHistorialBajaUsuarioBaja { get; set; } = new List<ProveedorHistorialBaja>();

    public virtual ICollection<ProveedorHistorialBaja> ProveedorHistorialBajaUsuarioReactivacion { get; set; } = new List<ProveedorHistorialBaja>();

    public virtual Rol Rol { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual ICollection<Transferencia> Transferencia { get; set; } = new List<Transferencia>();

    public virtual Usuario? UsuarioAlta { get; set; }

    public virtual Usuario? UsuarioBaja { get; set; }

    public virtual UsuarioHistorialBaja? UsuarioHistorialBajaUsuario { get; set; }

    public virtual ICollection<UsuarioHistorialBaja> UsuarioHistorialBajaUsuarioBaja { get; set; } = new List<UsuarioHistorialBaja>();

    public virtual ICollection<UsuarioHistorialBaja> UsuarioHistorialBajaUsuarioReactivacion { get; set; } = new List<UsuarioHistorialBaja>();
}

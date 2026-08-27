using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class Proveedor
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string RazonSocial { get; set; } = null!;

    public string? Cuit { get; set; }

    public bool CuitSimulado { get; set; }

    public string? CondicionFiscal { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// Página de precios del proveedor, usada por el comparador (HU-CMP-05). Ningún proveedor la tiene cargada en el sistema de origen: hay que relevarla.
    /// </summary>
    public string? UrlReferencia { get; set; }

    public bool Activo { get; set; }

    public string? MotivoBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaAlta { get; set; }

    public virtual ICollection<ArticuloCodigoAlternativo> ArticuloCodigoAlternativos { get; set; } = new List<ArticuloCodigoAlternativo>();

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompras { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<Articulo> Articulos { get; set; } = new List<Articulo>();

    public virtual ICollection<FacturaCompra> FacturaCompras { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<PrecioProveedor> PrecioProveedors { get; set; } = new List<PrecioProveedor>();

    public virtual ProveedorCredencial? ProveedorCredencial { get; set; }

    public virtual ICollection<ProveedorDireccion> ProveedorDireccions { get; set; } = new List<ProveedorDireccion>();

    public virtual ICollection<ProveedorTelefono> ProveedorTelefonos { get; set; } = new List<ProveedorTelefono>();
}

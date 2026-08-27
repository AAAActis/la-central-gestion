using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class FacturaCompra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Numero { get; set; } = null!;

    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public short SucursalId { get; set; }

    public decimal? RetencionIva { get; set; }

    public decimal? RetencionIibb { get; set; }

    public decimal? RetencionMunicipal { get; set; }

    public decimal? RetencionGanancias { get; set; }

    public string Estado { get; set; } = null!;

    public string Origen { get; set; } = null!;

    public int UsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? UsuarioAnulacionId { get; set; }

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompras { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalles { get; set; } = new List<FacturaCompraDetalle>();

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioAnulacion { get; set; }
}

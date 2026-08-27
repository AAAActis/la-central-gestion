using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class FacturaVentum
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string Numero { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public short SucursalId { get; set; }

    public string? OrdenCompra { get; set; }

    public string? Movil { get; set; }

    public string Estado { get; set; } = null!;

    public int UsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? UsuarioAnulacionId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<FacturaVentaDetalle> FacturaVentaDetalles { get; set; } = new List<FacturaVentaDetalle>();

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioAnulacion { get; set; }
}

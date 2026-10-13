using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Factura de compra de un proveedor. Importes en pesos argentinos: la empresa no opera en moneda extranjera (decisión D7, 07/10/2026). El sistema anterior admite moneda y cotización por comprobante y por línea (APS-005); acá queda excluido del alcance de forma deliberada. El destino de stock se carga por línea, no por comprobante (D9).
/// </summary>
public partial class FacturaCompra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Numero { get; set; } = null!;

    public DateOnly FechaEmision { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    /// <summary>
    /// Percepción de IVA que el proveedor adiciona al comprobante. Dato transcripto del PDF: el sistema no calcula alícuotas ni liquida impuestos.
    /// </summary>
    public decimal? PercepcionIva { get; set; }

    /// <summary>
    /// Percepción de Ingresos Brutos. Dato transcripto, sin cálculo por jurisdicción.
    /// </summary>
    public decimal? PercepcionIibb { get; set; }

    /// <summary>
    /// Percepción municipal. Dato transcripto.
    /// </summary>
    public decimal? PercepcionMunicipal { get; set; }

    /// <summary>
    /// Percepción de Ganancias. Dato transcripto.
    /// </summary>
    public decimal? PercepcionGanancias { get; set; }

    public string Estado { get; set; } = null!;

    public string Origen { get; set; } = null!;

    public int UsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? UsuarioAnulacionId { get; set; }

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompra { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalle { get; set; } = new List<FacturaCompraDetalle>();

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioAnulacion { get; set; }
}

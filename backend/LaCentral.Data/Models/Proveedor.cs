using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

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

    /// <summary>
    /// Tipo de comprobante que el proveedor suele emitir (A o B). Solo el valor por defecto de la cabecera de una factura de compra: un proveedor puede emitir A y B indistintamente (Sergio, 13/11/2026). NULL = sin dato.
    /// </summary>
    public char? TipoComprobanteHabitual { get; set; }

    /// <summary>
    /// Cómo vence una factura de este proveedor, desde su fecha de emisión: MANUAL (no se calcula; se carga a mano, por defecto), CONTADO (el mismo día), DIAS_CORRIDOS (emisión + vencimiento_dias) o DIA_FIJO_MES_SIGUIENTE (el día vencimiento_dias del mes siguiente a la emisión). Sergio vía Javi, 23/11/2026.
    /// </summary>
    public string VencimientoModo { get; set; } = null!;

    /// <summary>
    /// Los días de vencimiento_modo: NULL en MANUAL y CONTADO; 7, 10, 20, 21, 25 o 30 en DIAS_CORRIDOS; 1 a 28 (el día del mes) en DIA_FIJO_MES_SIGUIENTE. Sergio vía Javi, 23/11/2026.
    /// </summary>
    public short? VencimientoDias { get; set; }

    public virtual ICollection<Articulo> Articulo { get; set; } = new List<Articulo>();

    public virtual ICollection<ArticuloCodigoAlternativo> ArticuloCodigoAlternativo { get; set; } = new List<ArticuloCodigoAlternativo>();

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompra { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<FacturaCompra> FacturaCompra { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<NotaCreditoCompra> NotaCreditoCompra { get; set; } = new List<NotaCreditoCompra>();

    public virtual ICollection<PrecioProveedor> PrecioProveedor { get; set; } = new List<PrecioProveedor>();

    public virtual ICollection<ProveedorArticulo> ProveedorArticulo { get; set; } = new List<ProveedorArticulo>();

    public virtual ProveedorCredencial? ProveedorCredencial { get; set; }

    public virtual ICollection<ProveedorDireccion> ProveedorDireccion { get; set; } = new List<ProveedorDireccion>();

    public virtual ProveedorHistorialBaja? ProveedorHistorialBaja { get; set; }

    public virtual ICollection<ProveedorTelefono> ProveedorTelefono { get; set; } = new List<ProveedorTelefono>();

    public virtual ICollection<ScrapeCorrida> ScrapeCorrida { get; set; } = new List<ScrapeCorrida>();
}

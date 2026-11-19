using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Articulo
{
    public int Id { get; set; }

    public string CodigoInterno { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    /// <summary>
    /// DENORMALIZACIÓN DELIBERADA: valor derivado del último registro válido de articulo_historial_compra. Se mantiene acá para no recalcularlo en cada consulta del comparador.
    /// </summary>
    public decimal? PrecioCosto { get; set; }

    public int? UltimoProveedorId { get; set; }

    public bool Activo { get; set; }

    public string? MotivoBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaAlta { get; set; }

    /// <summary>
    /// EAN/UPC del fabricante. Identificador común de equivalencia entre proveedores: dos proveedores que venden la misma pieza traen el mismo código de barras, así que la equivalencia se deduce sin declararla. No reemplaza a articulo_codigo_alternativo (hay repuestos sin EAN, y marcas distintas tienen EAN distinto). VARCHAR y no numérico: los ceros a la izquierda cuentan. Pedido de la Dirección, 02/11/2026. Alimenta el reporte R-10.
    /// </summary>
    public string? CodigoBarras { get; set; }

    public virtual ICollection<ArticuloCodigoAlternativo> ArticuloCodigoAlternativo { get; set; } = new List<ArticuloCodigoAlternativo>();

    public virtual ArticuloHistorialBaja? ArticuloHistorialBaja { get; set; }

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompra { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<ArticuloPrecioModalidad> ArticuloPrecioModalidad { get; set; } = new List<ArticuloPrecioModalidad>();

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalle { get; set; } = new List<FacturaCompraDetalle>();

    public virtual ICollection<FacturaVentaDetalle> FacturaVentaDetalle { get; set; } = new List<FacturaVentaDetalle>();

    public virtual ICollection<PrecioProveedor> PrecioProveedor { get; set; } = new List<PrecioProveedor>();

    public virtual ICollection<Stock> Stock { get; set; } = new List<Stock>();

    public virtual ICollection<TransferenciaDetalle> TransferenciaDetalle { get; set; } = new List<TransferenciaDetalle>();

    public virtual Proveedor? UltimoProveedor { get; set; }
}

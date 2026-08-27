using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class Articulo
{
    public int Id { get; set; }

    public string CodigoInterno { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    /// <summary>
    /// DENORMALIZACIÓN DELIBERADA: valor derivado del último registro válido de articulo_historial_compra. Se mantiene acá para no recalcularlo en cada consulta del comparador.
    /// </summary>
    public decimal? PrecioCosto { get; set; }

    public decimal? PrecioVentaEstimado { get; set; }

    public int? UltimoProveedorId { get; set; }

    /// <summary>
    /// Posición física de referencia en el depósito, en texto libre. Dato informativo: el sistema no gestiona el depósito.
    /// </summary>
    public string? UbicacionDeposito { get; set; }

    public bool Activo { get; set; }

    public string? MotivoBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaAlta { get; set; }

    public virtual ICollection<ArticuloCodigoAlternativo> ArticuloCodigoAlternativos { get; set; } = new List<ArticuloCodigoAlternativo>();

    public virtual ICollection<ArticuloHistorialCompra> ArticuloHistorialCompras { get; set; } = new List<ArticuloHistorialCompra>();

    public virtual ICollection<ArticuloMargen> ArticuloMargens { get; set; } = new List<ArticuloMargen>();

    public virtual ICollection<FacturaCompraDetalle> FacturaCompraDetalles { get; set; } = new List<FacturaCompraDetalle>();

    public virtual ICollection<FacturaVentaDetalle> FacturaVentaDetalles { get; set; } = new List<FacturaVentaDetalle>();

    public virtual ICollection<PrecioProveedor> PrecioProveedors { get; set; } = new List<PrecioProveedor>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<TransferenciaDetalle> TransferenciaDetalles { get; set; } = new List<TransferenciaDetalle>();

    public virtual Proveedor? UltimoProveedor { get; set; }
}

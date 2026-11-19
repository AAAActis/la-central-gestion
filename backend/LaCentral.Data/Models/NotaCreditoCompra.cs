using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Nota de crédito de un proveedor (HU-FCO-07). Siempre tipo A: el número se copia exacto y el IVA es el 21 % del neto, que no se guarda. No cambia el costo de ningún artículo: es plata a favor en la cuenta corriente con el proveedor, que el sistema no lleva (Sergio, 18/11/2026).
/// </summary>
public partial class NotaCreditoCompra
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Numero { get; set; } = null!;

    public DateOnly FechaEmision { get; set; }

    /// <summary>
    /// BONIFICACION: bonificación o diferencia de precio, solo importes, no mueve stock. DEVOLUCION: devolución de mercadería, mueve stock (nota_credito_compra_detalle).
    /// </summary>
    public string Clase { get; set; } = null!;

    /// <summary>
    /// Importe neto del comprobante, sin IVA ni percepciones. En una devolución también se transcribe: las líneas mueven stock, no plata.
    /// </summary>
    public decimal ImporteNeto { get; set; }

    public decimal? PercepcionIva { get; set; }

    public decimal? PercepcionIibb { get; set; }

    public decimal? PercepcionMunicipal { get; set; }

    public decimal? PercepcionGanancias { get; set; }

    public string Origen { get; set; } = null!;

    public int UsuarioId { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string Estado { get; set; } = null!;

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? UsuarioAnulacionId { get; set; }

    public int? UsuarioAutorizacionId { get; set; }

    public virtual ICollection<NotaCreditoCompraDetalle> NotaCreditoCompraDetalle { get; set; } = new List<NotaCreditoCompraDetalle>();

    public virtual ICollection<NotaCreditoCompraFactura> NotaCreditoCompraFactura { get; set; } = new List<NotaCreditoCompraFactura>();

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioAnulacion { get; set; }

    public virtual Usuario? UsuarioAutorizacion { get; set; }
}

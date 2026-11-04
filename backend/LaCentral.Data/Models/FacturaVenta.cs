using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class FacturaVenta
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

    /// <summary>
    /// Cómo se cobró ESTA venta. Distinto de cliente.condicion_pago, que es la condición acordada con el cliente: uno de cuenta corriente puede pagar una venta en efectivo, y ninguno de los dos datos se deduce del otro. Pedido de la Dirección, 02/11/2026. Alimenta el reporte R-14.
    /// </summary>
    public string MedioCobro { get; set; } = null!;

    /// <summary>
    /// Quién AUTORIZÓ la anulación, que tiene que ser una persona distinta de quien la pidió (doble certificación exigida por la Dirección, 02/11/2026). Que además tenga rol ADMINISTRADOR lo valida el caso de uso: depende de otra tabla y un CHECK no puede mirarla. Alimenta R-08 y modifica el RF-010.
    /// </summary>
    public int? UsuarioAutorizacionId { get; set; }

    /// <summary>
    /// Indica si la operación se respaldó con comprobante fiscal. TRUE por defecto: es lo que el esquema afirmaba implícitamente al no tener la columna.
    /// </summary>
    public bool ConComprobanteFiscal { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual ICollection<FacturaVentaDetalle> FacturaVentaDetalle { get; set; } = new List<FacturaVentaDetalle>();

    public virtual Sucursal Sucursal { get; set; } = null!;

    public virtual Usuario Usuario { get; set; } = null!;

    public virtual Usuario? UsuarioAnulacion { get; set; }

    public virtual Usuario? UsuarioAutorizacion { get; set; }
}

using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Catálogo actual de cada proveedor, una fila por código del proveedor, que n8n reingesta en cada corrida con INSERT ... ON CONFLICT (proveedor_id, codigo) DO UPDATE (D-S7-01, 23/11/2026). La vinculación con un artículo propio es articulo_codigo_alternativo (mismo proveedor_id y codigo): no apunta al id de esta fila, así que reingestar no la pierde. HU-CMP-08 (055).
/// </summary>
public partial class ProveedorArticulo
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    /// <summary>
    /// Código comercial del artículo en el proveedor, el que viene en sus facturas (Expoyer: commercialCode, «C 000100 14 E»). Clave de la reingesta junto con proveedor_id, y el que se cruza con articulo_codigo_alternativo.codigo.
    /// </summary>
    public string Codigo { get; set; } = null!;

    /// <summary>
    /// Identificador del renglón en el portal del proveedor (Expoyer: internalCode, «C00010014E»).
    /// </summary>
    public string? IdExterno { get; set; }

    public string? CodigoBarras { get; set; }

    /// <summary>
    /// Número original de fabricante; puede traer varios separados por « / » (Expoyer: familyCode).
    /// </summary>
    public string? NumeroOriginal { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    /// <summary>
    /// Ficha técnica aplanada: «CLAVE: valor | CLAVE: valor».
    /// </summary>
    public string? DetalleTecnico { get; set; }

    /// <summary>
    /// Marca del PRODUCTO (Expoyer: supplier.name). No la del vehículo (brandName).
    /// </summary>
    public string? Marca { get; set; }

    /// <summary>
    /// Rubro del proveedor, en sus palabras (Expoyer: category.name). No es un rubro de La Central.
    /// </summary>
    public string? Rubro { get; set; }

    /// <summary>
    /// Costo neto, sin IVA, con el descuento del cliente (Expoyer: discountedPrice = lista × (1 − 61,08 %) × (1 − 15 %), validado contra su factura). Desconocido es NULL, nunca cero.
    /// </summary>
    public decimal? PrecioNeto { get; set; }

    /// <summary>
    /// Precio de lista del proveedor, sin IVA (Expoyer: basePrice).
    /// </summary>
    public decimal? PrecioLista { get; set; }

    /// <summary>
    /// Neto con pago anticipado, si el proveedor lo informa (Expoyer: earlyPaymentPrice). El precio de venta que muestra el portal (salePrice, neto + margen de la vista cliente) NO se guarda.
    /// </summary>
    public decimal? PrecioPagoAnticipado { get; set; }

    /// <summary>
    /// Disponibilidad que informa el proveedor en el depósito que le sirve a La Central (Expoyer: CORDOBA; 1 = HAY, 3 = NO_HAY, 2 = A_CONFIRMAR hasta que Sergio diga qué es). NULL = no informa.
    /// </summary>
    public string? EstadoStock { get; set; }

    /// <summary>
    /// Cantidad, solo si el proveedor la informa (Expoyer no). NULL = no informa, nunca cero.
    /// </summary>
    public decimal? Stock { get; set; }

    /// <summary>
    /// Alícuota que informa el proveedor (21 = 21 %). NULL = no la informa (Expoyer: vatRate null): el comparador usa la del artículo propio o, sin ella, 21 % (D-S7-02).
    /// </summary>
    public decimal? AlicuotaIva { get; set; }

    /// <summary>
    /// URL de la imagen en el sitio del proveedor. Las imágenes no se descargan.
    /// </summary>
    public string? UrlImagen { get; set; }

    public DateTime UltimaLectura { get; set; }

    /// <summary>
    /// Fecha que el sitio del proveedor da para el dato (actualización del precio), si la da.
    /// </summary>
    public DateTime? FechaOrigen { get; set; }

    /// <summary>
    /// Lo que dice el proveedor: FALSE si lo publica como discontinuado. Si no informa nada, TRUE.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>
    /// TRUE = no vino en la última corrida SUCCESS completa del proveedor. No se borra: conserva su vinculación y su historial, y vuelve a FALSE si reaparece (055 CA-003).
    /// </summary>
    public bool Vencido { get; set; }

    /// <summary>
    /// Última corrida que leyó este renglón. Al cerrar en SUCCESS, lo que no tenga esta corrida se marca vencido.
    /// </summary>
    public int UltimaCorridaId { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual ICollection<ProveedorArticuloHistorial> ProveedorArticuloHistorial { get; set; } = new List<ProveedorArticuloHistorial>();

    public virtual ScrapeCorrida ScrapeCorrida { get; set; } = null!;
}

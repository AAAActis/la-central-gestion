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
    /// Código del artículo en el proveedor. Clave de la reingesta junto con proveedor_id.
    /// </summary>
    public string Codigo { get; set; } = null!;

    public string? IdExterno { get; set; }

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    /// <summary>
    /// Ficha técnica aplanada: «CLAVE: valor | CLAVE: valor».
    /// </summary>
    public string? DetalleTecnico { get; set; }

    public string? Marca { get; set; }

    /// <summary>
    /// Precio neto, sin IVA. Desconocido es NULL, nunca cero.
    /// </summary>
    public decimal? PrecioNeto { get; set; }

    public decimal? PrecioLista { get; set; }

    public decimal? Stock { get; set; }

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
    /// FALSE = vencido: no vino en la última corrida SUCCESS del proveedor. No se borra: conserva su vinculación y su historial, y vuelve a TRUE si reaparece.
    /// </summary>
    public bool Activo { get; set; }

    /// <summary>
    /// Última corrida que leyó este renglón. Al cerrar en SUCCESS, lo que no tenga esta corrida se marca vencido.
    /// </summary>
    public int UltimaCorridaId { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual ICollection<ProveedorArticuloHistorial> ProveedorArticuloHistorial { get; set; } = new List<ProveedorArticuloHistorial>();

    public virtual ScrapeCorrida ScrapeCorrida { get; set; } = null!;
}

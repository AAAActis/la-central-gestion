using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Precios y stock de cada renglón del catálogo en cada corrida (RF-016). Una fila por (renglón, corrida): un renglón repetido dentro de la misma corrida (un rubro bajado en los dos sentidos) no duplica. Crece ~100.000 filas por corrida y proveedor. Las dos FK compuestas con proveedor_id impiden mezclar proveedores. HU-CMP-08 (055).
/// </summary>
public partial class ProveedorArticuloHistorial
{
    public int ProveedorArticuloId { get; set; }

    public int CorridaId { get; set; }

    public int ProveedorId { get; set; }

    public decimal? PrecioNeto { get; set; }

    public decimal? PrecioLista { get; set; }

    public decimal? PrecioPagoAnticipado { get; set; }

    public string? EstadoStock { get; set; }

    public decimal? Stock { get; set; }

    public DateTime FechaLectura { get; set; }

    public virtual ProveedorArticulo ProveedorArticulo { get; set; } = null!;

    public virtual ScrapeCorrida ScrapeCorrida { get; set; } = null!;
}

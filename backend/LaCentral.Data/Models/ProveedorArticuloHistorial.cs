using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Precio y stock de cada renglón del catálogo en cada corrida (RF-016). Una fila por (renglón, corrida): una página reenviada dentro de la misma corrida no duplica. Las dos FK compuestas con proveedor_id impiden mezclar proveedores. HU-CMP-08 (055).
/// </summary>
public partial class ProveedorArticuloHistorial
{
    public int ProveedorArticuloId { get; set; }

    public int CorridaId { get; set; }

    public int ProveedorId { get; set; }

    public decimal? PrecioNeto { get; set; }

    public decimal? PrecioLista { get; set; }

    public decimal? Stock { get; set; }

    public DateTime FechaLectura { get; set; }

    public virtual ProveedorArticulo ProveedorArticulo { get; set; } = null!;

    public virtual ScrapeCorrida ScrapeCorrida { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Cada lectura del catálogo de un proveedor que hace n8n (D-S7-01, 23/11/2026). Se abre en RUNNING y se cierra en SUCCESS o FAILED con la cantidad de ítems leídos (la cuenta del historial de la corrida). A lo sumo una RUNNING por proveedor. HU-CMP-08 (055).
/// </summary>
public partial class ScrapeCorrida
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string Estado { get; set; } = null!;

    public DateTime Inicio { get; set; }

    public DateTime? Fin { get; set; }

    /// <summary>
    /// Total de artículos que informó el sitio en el sondeo inicial (página de 1 artículo). Se compara con cantidad_items al cerrar.
    /// </summary>
    public int? TotalInformado { get; set; }

    public int? CantidadItems { get; set; }

    /// <summary>
    /// Id de la ejecución de n8n ($execution.id), para encontrarla en su historial.
    /// </summary>
    public string? EjecucionN8n { get; set; }

    public string? MensajeError { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual ICollection<ProveedorArticulo> ProveedorArticulo { get; set; } = new List<ProveedorArticulo>();

    public virtual ICollection<ProveedorArticuloHistorial> ProveedorArticuloHistorial { get; set; } = new List<ProveedorArticuloHistorial>();
}

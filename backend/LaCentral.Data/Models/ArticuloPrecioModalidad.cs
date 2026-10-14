using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// D1: precios de venta por articulo y modalidad, con el porcentaje del que salieron y la fecha en que se estamparon. Reemplaza a articulo_margen. La unica regla de calculo del sistema vive en CalcularPreciosArticuloUseCase.
/// </summary>
public partial class ArticuloPrecioModalidad
{
    public int ArticuloId { get; set; }

    /// <summary>
    /// Nombre exacto del miembro del enum Modalidad. Seis valores, sin id numerico.
    /// </summary>
    public string Modalidad { get; set; } = null!;

    /// <summary>
    /// Porcentaje sobre el costo. NULL si no hay formula (CasoEspecial) o la tasa no esta definida (Tarjeta3/Tarjeta6).
    /// </summary>
    public decimal? Porcentaje { get; set; }

    /// <summary>
    /// Precio estampado. NULL cuando no se pudo calcular. Nunca cero por defecto.
    /// </summary>
    public decimal? Precio { get; set; }

    /// <summary>
    /// true = lo fijo una persona. El recalculo automatico de la Fase 6 filtra por false y no lo pisa.
    /// </summary>
    public bool CalculoManual { get; set; }

    /// <summary>
    /// Cuando se estampo este precio.
    /// </summary>
    public DateTime? FechaCalculo { get; set; }

    /// <summary>
    /// Quien fijo el precio manual. NULL en los calculados.
    /// </summary>
    public int? UsuarioId { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }
}

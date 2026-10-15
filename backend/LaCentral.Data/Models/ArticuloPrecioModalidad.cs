using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// D1: precios de venta por articulo y modalidad. Formula: precio = valor_de_la_base * (1 + porcentaje/100). Lista = +50% sobre CostoArticulo. Costo = 0% sobre CostoArticulo. EfectivoTransferencia = -10% sobre PrecioLista. Tarjeta3/Tarjeta6: base y porcentaje NULL hasta que la tasa este definida. CasoEspecial: sin formula, precio manual. La unica regla de calculo del sistema vive en CalcularPreciosArticuloUseCase.
/// </summary>
public partial class ArticuloPrecioModalidad
{
    public int ArticuloId { get; set; }

    /// <summary>
    /// Nombre exacto del miembro del enum Modalidad. Seis valores, sin id numerico.
    /// </summary>
    public string Modalidad { get; set; } = null!;

    /// <summary>
    /// Porcentaje sobre el valor de `base`. Admite negativos hasta -100: EfectivoTransferencia es un descuento (-10) sobre PrecioLista. NULL si no hay formula (CasoEspecial) o la tasa no esta definida (Tarjeta3/Tarjeta6).
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

    /// <summary>
    /// Sobre que se aplica el porcentaje: CostoArticulo (el campo costo del articulo) o PrecioLista (el precio de la modalidad Lista del mismo articulo). NULL cuando no hay formula. OJO: CostoArticulo NO es la modalidad Costo.
    /// </summary>
    public string? Base { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Usuario? Usuario { get; set; }
}

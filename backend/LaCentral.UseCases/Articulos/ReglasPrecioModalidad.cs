using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Articulos;

/// <summary>
/// D1: la configuración con que nace un artículo (HU-ART-01). La regla de cálculo vive
/// en CalcularPreciosArticuloUseCase.
/// </summary>
public static class ReglasPrecioModalidad
{
    /// <summary>
    /// Decisión C: las seis filas con que nace un artículo, con precio null. Tarjeta3/6 sin
    /// base ni porcentaje hasta que existan las tasas: D1 prohíbe inventar un número.
    /// CasoEspecial es manual y no tiene fórmula. Filas nuevas en cada llamada.
    /// </summary>
    public static IReadOnlyList<PrecioModalidad> ConfiguracionInicial() => new PrecioModalidad[]
    {
        new() { Modalidad = Modalidad.Lista, Base = BasePrecio.CostoArticulo, Porcentaje = 50m },
        new() { Modalidad = Modalidad.Tarjeta3 },
        new() { Modalidad = Modalidad.Tarjeta6 },
        new() { Modalidad = Modalidad.EfectivoTransferencia, Base = BasePrecio.PrecioLista, Porcentaje = -10m },
        new() { Modalidad = Modalidad.Costo, Base = BasePrecio.CostoArticulo, Porcentaje = 0m },
        new() { Modalidad = Modalidad.CasoEspecial, CalculoManual = true },
    };
}

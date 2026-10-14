using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Articulos;

/// <summary>
/// D1: la única regla de cálculo de precios del sistema. La usan el alta de artículo
/// (HU-ART-01) y el recálculo al confirmar una factura de compra (Fase 6).
/// precio = valor_de_la_base × (1 + porcentaje / 100)
/// </summary>
public static class ReglasPrecioModalidad
{
    /// <summary>
    /// Null cuando no hay fórmula (manual, o tasa sin definir) o cuando falta el valor de
    /// la base. Nunca cero por defecto, y nunca se sustituye una base por otra.
    /// </summary>
    public static decimal? Calcular(PrecioModalidad configuracion, decimal? costoArticulo, decimal? precioLista)
    {
        if (configuracion.CalculoManual || configuracion.Porcentaje is not { } porcentaje)
            return null;

        var valorBase = configuracion.Base switch
        {
            BasePrecio.CostoArticulo => costoArticulo,
            BasePrecio.PrecioLista => precioLista,
            _ => null
        };
        if (valorBase is null)
            return null;

        return Math.Round(valorBase.Value * (1m + porcentaje / 100m), 2, MidpointRounding.AwayFromZero);
    }

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

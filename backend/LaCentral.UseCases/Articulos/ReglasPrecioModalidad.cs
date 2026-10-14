using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Articulos;

/// <summary>
/// D1: la única regla de cálculo de precios del sistema. La usan el alta de artículo
/// (HU-ART-01) y el recálculo al confirmar una factura de compra (Fase 6).
/// </summary>
public static class ReglasPrecioModalidad
{
    public static decimal? Calcular(PrecioModalidad configuracion, decimal? costoArticulo, decimal? precioLista)
        => null;

    public static IReadOnlyList<PrecioModalidad> ConfiguracionInicial()
        => Array.Empty<PrecioModalidad>();
}

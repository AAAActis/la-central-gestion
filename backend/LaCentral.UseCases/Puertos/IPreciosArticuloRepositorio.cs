using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// HU-ART-05 · lectura y escritura de los precios por modalidad para el recálculo. Usa el
/// DbContext scoped de la petición: dentro de una IUnidadDeTrabajo, ve lo que la operación
/// ya escribió (el costo de una compra) y lo que escribe se confirma o se descarta con ella.
/// </summary>
public interface IPreciosArticuloRepositorio
{
    /// <summary>
    /// Costo vigente y filas de articulo_precio_modalidad de cada artículo pedido. Los que
    /// no existen no aparecen; uno sin filas aparece con la lista vacía.
    /// </summary>
    Task<IReadOnlyDictionary<int, PreciosDeArticulo>> ObtenerAsync(IReadOnlyCollection<int> articuloIds, CancellationToken ct = default);

    /// <summary>
    /// Escribe precio y fecha de cálculo de cada fila, por artículo y modalidad. No toca
    /// base, porcentaje, calculo_manual ni usuario, y no escribe las filas que en la base
    /// son manuales al momento de escribir: un precio fijado por una persona no se pisa.
    /// </summary>
    Task ActualizarPreciosAsync(int articuloId, IReadOnlyList<PrecioModalidad> precios, CancellationToken ct = default);
}

/// <summary>Lo que el recálculo necesita de un artículo.</summary>
public sealed record PreciosDeArticulo(decimal? CostoVigente, IReadOnlyList<PrecioModalidad> Precios);

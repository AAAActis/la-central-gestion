using LaCentral.UseCases.Catalogo;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// 054 · Vinculación de una fila del catálogo de un proveedor (proveedor_articulo) con un artículo
/// propio. La vinculación es la fila de articulo_codigo_alternativo (D-S7-07): (proveedor_id,
/// codigo, articulo_id), con usuario_id y fecha_vinculacion (script 16).
/// </summary>
public interface ICatalogoVinculacionRepositorio
{
    /// <summary>La fila de catálogo; null si no existe.</summary>
    Task<FilaParaVincularDto?> ObtenerFilaAsync(int filaId, CancellationToken ct = default);

    /// <summary>null si el artículo no existe; si existe, si está activo.</summary>
    Task<bool?> ArticuloActivoAsync(int articuloId, CancellationToken ct = default);

    /// <summary>El artículo al que ya está vinculado (proveedor_id, codigo); null si a ninguno.</summary>
    Task<ArticuloVinculadoDto?> VinculadoAAsync(int proveedorId, string codigo, CancellationToken ct = default);

    /// <summary>
    /// Inserta el código alternativo con el usuario y la fecha, en una sola fila (CA1). false si la
    /// unicidad (proveedor_id, codigo) lo rechazó: otro lo vinculó entre la consulta y la escritura.
    /// </summary>
    Task<bool> VincularAsync(int articuloId, int proveedorId, string codigo, int usuarioId, CancellationToken ct = default);
}

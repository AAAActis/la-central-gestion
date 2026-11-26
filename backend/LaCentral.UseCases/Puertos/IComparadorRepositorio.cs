using LaCentral.UseCases.Comparador;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// 030 · Las filas de catálogo vinculadas a un artículo, por (proveedor_id, código) de
/// articulo_codigo_alternativo (D-S7-07). Sin proveedores dados de baja (030 CA-005) ni vencidos (D-S7-10).
/// </summary>
public interface IComparadorRepositorio
{
    /// <summary>null si el artículo no existe.</summary>
    Task<DatosComparador?> ObtenerAsync(int articuloId, CancellationToken ct = default);
}

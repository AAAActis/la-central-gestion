using LaCentral.UseCases.Catalogo;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// 053 · Candidatos de vinculación sobre el catálogo de los proveedores (proveedor_articulo). Nunca
/// devuelve filas vencidas (D-S7-10) ni de proveedores dados de baja (CA3).
/// </summary>
public interface ICatalogoCandidatosRepositorio
{
    /// <summary>El nombre del artículo y el de las filas de catálogo ya vinculadas a él (CA5); null si no existe.</summary>
    Task<IReadOnlyList<string>?> TextosDeArticuloAsync(int articuloId, CancellationToken ct = default);

    /// <summary>Filas cuyo código, número original, código de barras o nombre normalizado es la clave (CA2).</summary>
    Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarExactasAsync(string clave, CancellationToken ct = default);

    /// <summary>Filas cuyo nombre o número original se parece a alguno de los textos, de mayor a menor parecido.</summary>
    Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarAproximadasAsync(
        IReadOnlyList<string> textos, double umbral, int limite, CancellationToken ct = default);
}

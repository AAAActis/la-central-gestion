using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// Escritura del catálogo (HU-ART-01). La lectura y la búsqueda están en
/// IConsultaArticulosRepositorio.
/// </summary>
public interface IArticuloRepositorio
{
    /// <summary>
    /// Persiste el artículo con sus códigos, existencias y precios por modalidad, y
    /// devuelve el Id que generó la base. El código interno queda provisional hasta
    /// AsignarCodigoInternoAsync, en la misma unidad de trabajo.
    /// </summary>
    Task<int> AgregarAsync(Articulo articulo, CancellationToken ct = default);

    /// <summary>false si no existe un artículo con ese Id.</summary>
    Task<bool> AsignarCodigoInternoAsync(int id, string codigoInterno, CancellationToken ct = default);

    /// <summary>Las sucursales en las que un artículo nuevo tiene stock (en cero).</summary>
    Task<IReadOnlyList<short>> ObtenerSucursalesAsync(CancellationToken ct = default);
}

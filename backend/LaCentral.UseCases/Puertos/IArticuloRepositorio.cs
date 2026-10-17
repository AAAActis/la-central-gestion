using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// Escritura del catálogo (HU-ART-01 y HU-ART-03). La lectura y la búsqueda están en
/// IConsultaArticulosRepositorio. Todo se escribe por Id, nunca por código: un código es
/// mutable (lección del Crítico 3).
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

    /// <summary>
    /// Nombre, ubicación y, si no es null, código interno. Con null la columna no se toca: no
    /// se pisa un cambio que otra sesión hizo entre la lectura y la escritura. false si no hay
    /// un artículo ACTIVO con ese Id: la condición va en la escritura, así que vale lo que la
    /// fila es al escribir.
    /// </summary>
    Task<bool> ActualizarDatosAsync(int id, string nombre, string? ubicacionDeposito, string? codigoInterno,
        CancellationToken ct = default);

    /// <summary>Agrega códigos de proveedor sin tocar los que el artículo ya tiene.</summary>
    Task AgregarCodigosAsync(int id, IReadOnlyList<CodigoAlternativo> codigos, CancellationToken ct = default);

    /// <summary>Borra el código de ese proveedor (D2: hay uno solo). false si no había.</summary>
    Task<bool> RetirarCodigoAsync(int id, int proveedorId, CancellationToken ct = default);

    /// <summary>false si no hay un artículo ACTIVO con ese Id.</summary>
    Task<bool> DarDeBajaAsync(int id, string motivo, DateTime fechaBaja, CancellationToken ct = default);

    /// <summary>
    /// false si no hay un artículo INACTIVO con ese Id. Conserva motivo y fecha de la
    /// última baja en la fila (D10).
    /// </summary>
    Task<bool> ReactivarAsync(int id, CancellationToken ct = default);
}

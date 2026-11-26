using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IProveedorRepositorio
{
    Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default);
    Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default);
    Task AgregarAsync(Proveedor proveedor, CancellationToken ct = default);

    Task<Proveedor?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default);

    /// <summary>
    /// Fix E2 (D-S7-12): los proveedores ACTIVOS y EN NEGRO (<c>cuit_simulado</c>) cuya razón social contiene todas
    /// las palabras de <paramref name="texto"/> (sin acentos ni mayúsculas), en orden de Id. Devuelve a lo sumo
    /// <paramref name="limite"/>: quien llama pide uno más de los que necesita para saber si la coincidencia es única.
    /// </summary>
    Task<IReadOnlyList<Proveedor>> BuscarActivosEnNegroPorRazonSocialAsync(string texto, int limite, CancellationToken ct = default);

    /// <summary>ALTO 5: el código es la clave de negocio; devuelve el dueño para informar el conflicto.</summary>
    Task<Proveedor?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    /// <summary>CRÍTICO 3: por Id; false si la fila no existe.</summary>
    Task<bool> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default);

    /// <summary>
    /// HU-PRO-02: busca proveedores por Razón Social (coincidencia difusa) o CUIT
    /// (coincidencia exacta). El filtro de activos/inactivos se resuelve en la
    /// consulta, no en memoria. Mismo criterio que IClienteRepositorio.BuscarAsync.
    /// </summary>
    /// #147: una página (1 en adelante) y el total de coincidencias, en un orden estable.
    Task<(IReadOnlyList<Proveedor> Items, int Total)> BuscarAsync(
        string texto, bool incluirInactivos, int pagina, int tamanoPagina, bool porCuit, CancellationToken ct = default);
}
using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IProveedorRepositorio
{
    Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default);
    Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default);
    Task AgregarAsync(Proveedor proveedor, CancellationToken ct = default);

    Task<Proveedor?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default);

    /// <summary>ALTO 5: el código es la clave de negocio; devuelve el dueño para informar el conflicto.</summary>
    Task<Proveedor?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);
    /// <summary>CRÍTICO 3: por Id; false si la fila no existe.</summary>
    Task<bool> ActualizarAsync(Proveedor proveedor, CancellationToken ct = default);

    /// <summary>
    /// HU-PRO-02: busca proveedores por Razón Social (coincidencia difusa) o CUIT
    /// (coincidencia exacta). El filtro de activos/inactivos se resuelve en la
    /// consulta, no en memoria. Mismo criterio que IClienteRepositorio.BuscarAsync.
    /// </summary>
    Task<IReadOnlyList<Proveedor>> BuscarAsync(string texto, bool incluirInactivos, CancellationToken ct = default);
}
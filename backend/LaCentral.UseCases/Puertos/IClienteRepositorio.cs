
using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IClienteRepositorio
{
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default);
    Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default);
    Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default);
    Task AgregarAsync(Cliente cliente, CancellationToken ct = default);

    /// <summary>
    /// HU-CLI-02: busca clientes por Razón Social (coincidencia difusa) o CUIT/CUIL
    /// (coincidencia exacta). El filtro de activos/inactivos se resuelve en la consulta,
    /// no en memoria.
    /// </summary>
    Task<IReadOnlyList<Cliente>> BuscarAsync(string texto, bool incluirInactivos, CancellationToken ct = default);
    
    /// <summary>
    /// HU-CLI-02 (CA-005): Obtiene un cliente por ID incluyendo la totalidad 
    /// de sus teléfonos y direcciones (entidades 1:N).
    /// </summary>
    Task<Cliente?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);

    Task<Cliente?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default);
    Task ActualizarAsync(Cliente cliente, CancellationToken ct = default);
}
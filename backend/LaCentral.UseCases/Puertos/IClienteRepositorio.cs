
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
}
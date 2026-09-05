
using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IClienteRepositorio
{
    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken ct = default);
    Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default);
    Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default);
    Task AgregarAsync(Cliente cliente, CancellationToken ct = default);
}
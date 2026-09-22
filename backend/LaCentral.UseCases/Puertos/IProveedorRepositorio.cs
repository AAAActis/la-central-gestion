using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IProveedorRepositorio
{
    Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default);
    Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default);
    Task AgregarAsync(Proveedor proveedor, CancellationToken ct = default);

    Task<Proveedor?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);
    Task<Proveedor?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default);
    Task ActualizarAsync(Proveedor proveedor, CancellationToken ct = default);
}
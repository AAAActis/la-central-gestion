using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    // Hallazgo 8: Cambio a Task<bool> para informar si el registro se encontró y se actualizó.
    Task<bool> ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default);
}
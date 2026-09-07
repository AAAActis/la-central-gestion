using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>Busca por identificador. Devuelve null si no existe.</summary>
    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios de un usuario ya existente.</summary>
    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuántos administradores activos hay. Lo necesita la regla que impide
    /// dejar el sistema sin administración (CA4 de HU-ACC-02).
    /// </summary>
    Task<int> ContarAdministradoresActivosAsync(CancellationToken cancellationToken = default);
}

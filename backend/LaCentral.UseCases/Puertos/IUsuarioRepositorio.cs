using LaCentral.UseCases.Entidades; // Ahora el namespace matchea con lo que generó EF Core

namespace LaCentral.UseCases.Puertos;

public interface IUsuarioRepositorio
{
    Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default);
    Task CrearAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
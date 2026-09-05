using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

/// <summary>
/// Reactivación de un usuario dado de baja. Complementa CA3 de HU-ACC-02.
/// </summary>
public class ReactivarUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarios;

    public ReactivarUsuarioUseCase(IUsuarioRepositorio usuarios)
    {
        _usuarios = usuarios;
    }

    public async Task<Result> EjecutarAsync(
        int idAReactivar, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(idAReactivar, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure(TipoError.NoEncontrado,
                "El usuario indicado no existe.");
        }

        if (usuario.Activo)
        {
            return Result.Failure(TipoError.Conflicto,
                "El usuario ya se encuentra activo.");
        }

        // La otra mitad de la restricción: al reactivar hay que limpiar los
        // dos campos de baja, o la base rechaza el UPDATE.
        usuario.Activo = true;
        usuario.MotivoBaja = null;
        usuario.FechaBaja = null;

        await _usuarios.ActualizarAsync(usuario, cancellationToken);

        return Result.Success();
    }
}

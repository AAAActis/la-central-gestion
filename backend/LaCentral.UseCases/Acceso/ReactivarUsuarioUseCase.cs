using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

/// <summary>
/// Reactivación de un usuario dado de baja. Complementa CA3 de HU-ACC-02.
/// </summary>
public class ReactivarUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ReactivarUsuarioUseCase(IUsuarioRepositorio usuarios, IHistorialBajaRepositorio historial,
        IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _usuarios = usuarios;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
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

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // La fila se limpia; el motivo, la fecha y quién dio de baja quedan en el historial D10.
        usuario.Activo = true;
        usuario.MotivoBaja = null;
        usuario.FechaBaja = null;
        usuario.UsuarioBajaId = null;

        return await _unidadDeTrabajo.EjecutarAsync(async ct =>
        {
            if (!await _usuarios.ActualizarAsync(usuario, ct))
                return Result.Failure(TipoError.NoEncontrado, "El usuario indicado no existe.");

            await _historial.CerrarAsync(EntidadConHistorialBaja.Usuario, usuario.Id, DateTime.UtcNow, usuarioId, ct);
            return Result.Success();
        }, cancellationToken);
    }
}

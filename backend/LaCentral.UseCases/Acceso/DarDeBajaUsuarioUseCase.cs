using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

/// <summary>
/// Baja lógica de un usuario. Cubre CA3 y CA4 de HU-ACC-02.
///
/// La fila nunca se borra: el historial tiene que seguir mostrando quién
/// ejecutó cada operación anterior de ese puesto.
/// </summary>
public class DarDeBajaUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IContextoUsuario _contexto;

    // rol_id 2 = ADMINISTRADOR. Anotado para la retro: debería salir de la
    // tabla `rol` y no de una constante.
    private const int RolAdministrador = 2;

    public DarDeBajaUsuarioUseCase(IUsuarioRepositorio usuarios, IContextoUsuario contexto)
    {
        _usuarios = usuarios;
        _contexto = contexto;
    }

    public async Task<Result> EjecutarAsync(
        int idABajar, string motivo, CancellationToken cancellationToken = default)
    {
        // REGLA 3 — el motivo es obligatorio.
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Result.Failure(TipoError.Invalido,
                "El motivo de la baja es obligatorio.");
        }

        // El contexto devuelve 0 cuando no hay identidad en la petición. Sin
        // esta guarda, la regla 1 compararía contra 0, nunca coincidiría, y
        // dejaría de aplicarse sin que se note.
        var idEnSesion = _contexto.UsuarioId;
        if (idEnSesion <= 0)
        {
            return Result.Failure(TipoError.NoAutorizado,
                "No se pudo determinar el usuario que ejecuta la operación.");
        }

        // REGLA 1 — nadie se da de baja a sí mismo.
        if (idABajar == idEnSesion)
        {
            return Result.Failure(TipoError.Invalido,
                "Un usuario no puede darse de baja a sí mismo.");
        }

        var usuario = await _usuarios.ObtenerPorIdAsync(idABajar, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure(TipoError.NoEncontrado,
                "El usuario indicado no existe.");
        }

        if (!usuario.Activo)
        {
            return Result.Failure(TipoError.Conflicto,
                "El usuario ya se encuentra dado de baja.");
        }

        // REGLA 2 — siempre tiene que quedar un administrador activo.
        // Se consulta solo si el usuario a bajar es administrador: si es
        // operador, la regla no aplica y la consulta sería un viaje al pedo.
        if (usuario.RolId == RolAdministrador)
        {
            var administradoresActivos = await _usuarios
                .ContarAdministradoresActivosAsync(cancellationToken);

            if (administradoresActivos <= 1)
            {
                return Result.Failure(TipoError.Conflicto,
                    "No se puede dar de baja al único administrador activo: " +
                    "el sistema quedaría sin administración.");
            }
        }

        // Los tres campos se mueven juntos, porque la base tiene una
        // restricción que exige que motivo_baja y fecha_baja estén cargados
        // cuando activo es falso.
        usuario.Activo = false;
        usuario.MotivoBaja = motivo.Trim();
        usuario.FechaBaja = DateTime.UtcNow;

        await _usuarios.ActualizarAsync(usuario, cancellationToken);

        return Result.Success();
    }
}

using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

public class ReactivarClienteUseCase
{
    private readonly IClienteRepositorio _repositorio;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ReactivarClienteUseCase(IClienteRepositorio repositorio, IHistorialBajaRepositorio historial,
        IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _repositorio = repositorio;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public async Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (cliente is null)
            return Result.Failure(TipoError.NoEncontrado, "El cliente no existe.");

        if (cliente.Activo)
            return Result.Failure(TipoError.Invalido, "El cliente ya está activo.");

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // CA-002 y CA-003: reactivar manteniendo el historial del motivo
        cliente.Activo = true;
        // OJO: No blanqueamos MotivoBaja ni FechaBaja, quedan en la entidad.

        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (!await _repositorio.ActualizarAsync(cliente, c))
                return Result.Failure(TipoError.NoEncontrado, "El cliente no existe.");

            await _historial.CerrarAsync(EntidadConHistorialBaja.Cliente, cliente.Id, DateTime.UtcNow, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}
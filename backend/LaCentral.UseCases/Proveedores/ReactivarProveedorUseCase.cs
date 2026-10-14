using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Proveedores;

public class ReactivarProveedorUseCase
{
    private readonly IProveedorRepositorio _repositorio;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ReactivarProveedorUseCase(IProveedorRepositorio repositorio, IHistorialBajaRepositorio historial,
        IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _repositorio = repositorio;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public async Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var proveedor = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (proveedor is null)
        {
            return Result.Failure(TipoError.NoEncontrado, "El proveedor solicitado no existe.");
        }

        if (proveedor.Activo)
        {
            return Result.Failure(TipoError.Invalido, "El proveedor ya se encuentra activo.");
        }

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // Restauramos el estado activo conservando MotivoBaja como historial
        proveedor.Activo = true;

        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            await _repositorio.ActualizarAsync(proveedor, c);
            await _historial.CerrarAsync(EntidadConHistorialBaja.Proveedor, proveedor.Id, DateTime.UtcNow, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}
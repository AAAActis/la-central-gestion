using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

public class ReactivarClienteUseCase
{
    private readonly IClienteRepositorio _repositorio;

    public ReactivarClienteUseCase(IClienteRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (cliente is null)
            return Result.Failure("El cliente no existe.");

        if (cliente.Activo)
            return Result.Failure("El cliente ya está activo.");

        // CA-002 y CA-003: reactivar manteniendo el historial del motivo
        cliente.Activo = true;
        // OJO: No blanqueamos MotivoBaja ni FechaBaja, quedan en la entidad.

        await _repositorio.ActualizarAsync(cliente, ct);

        return Result.Success();
    }
}
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

public class DarDeBajaClienteUseCase
{
    private readonly IClienteRepositorio _repositorio;

    public DarDeBajaClienteUseCase(IClienteRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result> EjecutarAsync(int id, string cuitReescrito, string motivo, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (cliente is null)
            return Result.Failure("El cliente no existe.");

        if (!cliente.Activo)
            return Result.Failure("El cliente ya está inactivo.");

        // CA-002: confirmación por reescritura
        if (cliente.Cuit != cuitReescrito)
            return Result.Failure("El CUIT/CUIL no coincide. Verifique antes de confirmar.");

        // CA-003: motivo obligatorio
        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure("El motivo de baja es obligatorio.");

        // CA-001 y CA-004: aplicar baja lógica
        cliente.Activo = false;
        cliente.MotivoBaja = motivo;
        cliente.FechaBaja = DateTime.UtcNow;

        await _repositorio.ActualizarAsync(cliente, ct);

        return Result.Success();
    }
}
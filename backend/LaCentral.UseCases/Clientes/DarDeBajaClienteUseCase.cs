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
            return Result.Failure(TipoError.NoEncontrado, "El cliente no existe.");

        if (!cliente.Activo)
            return Result.Failure(TipoError.Invalido, "El cliente ya está inactivo.");
        
        string valorEsperado = string.IsNullOrWhiteSpace(cliente.Cuit) ? cliente.Codigo : cliente.Cuit;
        // CA-002: confirmación por reescritura
        if (valorEsperado != cuitReescrito)
            return Result.Failure(TipoError.Invalido, "El valor reescrito no coincide con el código o CUIT del cliente.");

        // CA-003: motivo obligatorio
        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure(TipoError.Invalido, "El motivo de baja es obligatorio.");

        // CA-001 y CA-004: aplicar baja lógica
        cliente.Activo = false;
        cliente.MotivoBaja = motivo;
        cliente.FechaBaja = DateTime.UtcNow;

        await _repositorio.ActualizarAsync(cliente, ct);

        return Result.Success();
    }
}
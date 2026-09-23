using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Proveedores;

public class ReactivarProveedorUseCase
{
    private readonly IProveedorRepositorio _repositorio;

    public ReactivarProveedorUseCase(IProveedorRepositorio repositorio)
    {
        _repositorio = repositorio;
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

        // Restauramos el estado activo conservando MotivoBaja como historial
        proveedor.Activo = true;

        await _repositorio.ActualizarAsync(proveedor, ct);

        return Result.Success();
    }
}
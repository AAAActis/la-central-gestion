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

        // Restauración del estado
        proveedor.Activo = true;
        proveedor.MotivoBaja = null;

        // Limpiamos el sufijo de baja del CUIT
        var indiceBaja = proveedor.Cuit.IndexOf("-BAJA", StringComparison.Ordinal);
        if (indiceBaja >= 0)
        {
            var cuitLimpio = proveedor.Cuit[..indiceBaja];
            
            // Verificamos si el CUIT original no fue tomado por otro proveedor activo en el interín
            var cuitEnUso = await _repositorio.ExisteCuitAsync(cuitLimpio, ct);
            if (cuitEnUso)
            {
                 return Result.Failure(TipoError.Conflicto, "No se puede reactivar: el CUIT original ya fue registrado por otro proveedor.");
            }
            
            proveedor.Cuit = cuitLimpio;
        }

        await _repositorio.ActualizarAsync(proveedor, ct);

        return Result.Success();
    }
}
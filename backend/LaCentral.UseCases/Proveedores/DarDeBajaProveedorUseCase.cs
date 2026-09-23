using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Proveedores;

public class DarDeBajaProveedorUseCase
{
    private readonly IProveedorRepositorio _repositorio;

    public DarDeBajaProveedorUseCase(IProveedorRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<Result> EjecutarAsync(int id, DarDeBajaProveedorRequest request, CancellationToken ct = default)
    {
        // CA-003: Motivo obligatorio
        if (string.IsNullOrWhiteSpace(request.MotivoBaja))
        {
            return Result.Failure(TipoError.Invalido, "Debe especificar un motivo para la baja del proveedor.");
        }

        var proveedor = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (proveedor is null)
        {
            return Result.Failure(TipoError.NoEncontrado, "El proveedor solicitado no existe.");
        }

        if (!proveedor.Activo)
        {
            return Result.Failure(TipoError.Invalido, "El proveedor ya se encuentra dado de baja.");
        }

        // CA-001 & CA-002: Baja lógica, asignación de motivo y reescritura de CUIT para liberar unicidad
        proveedor.Activo = false;
        proveedor.MotivoBaja = request.MotivoBaja;
        
        // Se anexa un sufijo al CUIT original para que el unique index de la DB permita dar de alta otro proveedor con el mismo CUIT si fuera necesario.
        if (!proveedor.Cuit.Contains("-BAJA"))
        {
            proveedor.Cuit = $"{proveedor.Cuit}-BAJA-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        await _repositorio.ActualizarAsync(proveedor, ct);

        return Result.Success();
    }
}
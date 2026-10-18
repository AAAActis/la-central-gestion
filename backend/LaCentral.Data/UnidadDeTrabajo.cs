using LaCentral.Data.Models;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data;

/// <summary>
/// Transacción sobre el DbContext scoped que comparten los repositorios de la petición:
/// todos los SaveChanges y ExecuteUpdate de la operación quedan adentro.
/// </summary>
public class UnidadDeTrabajo : IUnidadDeTrabajo
{
    private readonly LaCentralDbContext _context;

    public UnidadDeTrabajo(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task<Result> EjecutarAsync(Func<CancellationToken, Task<Result>> operacion, CancellationToken ct = default)
    {
        // Anidada: la confirma (o descarta) la unidad de trabajo exterior.
        if (_context.Database.CurrentTransaction is not null)
            return await operacion(ct);

        // Si la operación lanza, el Dispose sin Commit revierte todo.
        await using var transaccion = await _context.Database.BeginTransactionAsync(ct);
        var resultado = await operacion(ct);

        if (resultado.IsSuccess)
            await transaccion.CommitAsync(ct);
        else
            await transaccion.RollbackAsync(ct);

        return resultado;
    }
}

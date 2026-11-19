using LaCentral.Data.Models;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

/// <summary>HU-FCO-07 (057) · Nota de crédito de proveedor.</summary>
public class NotaCreditoCompraRepositorio : INotaCreditoCompraRepositorio
{
    private readonly LaCentralDbContext _context;

    public NotaCreditoCompraRepositorio(LaCentralDbContext context) => _context = context;

    public Task<bool> ExisteNumeroAsync(int proveedorId, string numero, CancellationToken ct = default)
        => Task.FromResult(false);

    public Task<IReadOnlyList<FacturaParaNotaCredito>> ObtenerFacturasAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<FacturaParaNotaCredito>>([]);

    public Task<IReadOnlyList<CompradoYDevuelto>> ObtenerCompradoYDevueltoAsync(IReadOnlyCollection<int> facturaIds, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CompradoYDevuelto>>([]);

    public Task<Result<int>> RegistrarConEfectosAsync(LaCentral.UseCases.Entidades.NotaCreditoCompra nota, CancellationToken ct = default)
        => Task.FromResult(Result<int>.Success(0));

    public Task<IReadOnlyList<string>> ObtenerVigentesDeFacturaAsync(int facturaId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<NotaCreditoParaAnular?> ObtenerParaAnularAsync(int id, CancellationToken ct = default)
        => Task.FromResult<NotaCreditoParaAnular?>(null);

    public Task<Result> AnularConEfectosAsync(AnulacionNotaCreditoCompra anulacion, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

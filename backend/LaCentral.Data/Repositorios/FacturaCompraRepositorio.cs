using LaCentral.Data.Models;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Repositorios;

public class FacturaCompraRepositorio : IFacturaCompraRepositorio
{
    private readonly LaCentralDbContext _context;

    public FacturaCompraRepositorio(LaCentralDbContext context) => _context = context;

    public Task<FacturaCompraExistente?> ObtenerNumeroActivoAsync(int proveedorId, string numero, CancellationToken ct = default)
        => Task.FromResult<FacturaCompraExistente?>(null);

    public Task<IReadOnlyDictionary<int, bool>> ObtenerEstadoArticulosAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<int, bool>>(new Dictionary<int, bool>());

    public Task<int> RegistrarConEfectosAsync(LaCentral.UseCases.Entidades.FacturaCompra factura, CancellationToken ct = default)
        => Task.FromResult(0);
}

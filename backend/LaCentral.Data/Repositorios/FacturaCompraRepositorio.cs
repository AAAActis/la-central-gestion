using LaCentral.Data.Models;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

public class FacturaCompraRepositorio : IFacturaCompraRepositorio
{
    // Valores de los CHECK fc_estado_valido y fc_origen_valido.
    private const string EstadoActiva = "ACTIVA";
    private const string EstadoAnulada = "ANULADA";
    private const string OrigenManual = "MANUAL";
    private const string OrigenPdf = "PDF";
    // stock.cantidad es NUMERIC(12,3): la parte entera tiene a lo sumo 9 dígitos.
    private const decimal LimiteStock = 1_000_000_000m;

    private readonly LaCentralDbContext _context;

    public FacturaCompraRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<FacturaCompraExistente?> ObtenerNumeroActivoAsync(int proveedorId, string numero, CancellationToken ct = default)
        => await _context.FacturaCompra.AsNoTracking()
            .Where(f => f.ProveedorId == proveedorId && f.Numero == numero && f.Estado == EstadoActiva)
            .Select(f => new FacturaCompraExistente(f.Id, f.Numero, f.FechaEmision))
            .FirstOrDefaultAsync(ct);

    public Task<short?> ObtenerIdReclamosAsync(CancellationToken ct = default)
        => Task.FromResult<short?>(null);

    public async Task<IReadOnlyDictionary<int, bool>> ObtenerEstadoArticulosAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        => await _context.Articulo.AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Activo, ct);

    /// <summary>
    /// Varias sentencias (SaveChanges, el upsert de stock, ExecuteUpdate): son atómicas
    /// solo porque el caso de uso las ejecuta dentro de IUnidadDeTrabajo, que abre UNA
    /// transacción sobre este mismo DbContext.
    /// </summary>
    public async Task<Result<int>> RegistrarConEfectosAsync(LaCentral.UseCases.Entidades.FacturaCompra factura, CancellationToken ct = default)
    {
        var fila = new FacturaCompra
        {
            ProveedorId = factura.ProveedorId,
            Numero = factura.Numero,
            FechaEmision = factura.FechaEmision,
            FechaVencimiento = factura.FechaVencimiento,
            FechaImputacion = factura.FechaImputacion,
            TipoComprobante = factura.TipoComprobante[0],
            PercepcionIva = factura.PercepcionIva,
            PercepcionIibb = factura.PercepcionIibb,
            PercepcionMunicipal = factura.PercepcionMunicipal,
            PercepcionGanancias = factura.PercepcionGanancias,
            Estado = EstadoActiva,
            Origen = factura.DesdePdf ? OrigenPdf : OrigenManual,
            UsuarioId = factura.UsuarioId,
            FechaRegistro = factura.FechaRegistro,
            FacturaCompraDetalle = factura.Lineas.Select(l => new FacturaCompraDetalle
            {
                ArticuloId = l.ArticuloId,
                SucursalId = l.SucursalId,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                DescuentoPorcentaje = l.DescuentoPorcentaje,
                Descuento2Porcentaje = l.Descuento2Porcentaje,
                Descuento3Porcentaje = l.Descuento3Porcentaje,
                Descuento4Porcentaje = l.Descuento4Porcentaje
            }).ToList()
        };
        _context.FacturaCompra.Add(fila);
        await _context.SaveChangesAsync(ct);

        foreach (var linea in factura.Lineas)
        {
            // D9: suma en el destino de la línea. El incremento lo hace el motor, así dos
            // compras simultáneas del mismo artículo no se pisan; si no había fila, se crea.
            // Si la suma no entra en stock.cantidad, el WHERE no actualiza (0 filas) en lugar
            // de desbordar (22003): es Invalido y la unidad de trabajo descarta todo.
            var filas = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO stock (articulo_id, sucursal_id, cantidad)
                VALUES ({linea.ArticuloId}, {linea.SucursalId}, {linea.Cantidad})
                ON CONFLICT (articulo_id, sucursal_id)
                DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad
                WHERE stock.cantidad + EXCLUDED.cantidad < {LimiteStock}
                """, ct);
            if (filas == 0)
                return Result<int>.Failure(TipoError.Invalido,
                    $"El stock del artículo {linea.ArticuloId} en la sucursal {linea.SucursalId} superaría el máximo que se puede registrar.");

            // Por Id, nunca por código (Crítico 3).
            await _context.Articulo.Where(a => a.Id == linea.ArticuloId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.PrecioCosto, linea.CostoUnitario)
                    .SetProperty(a => a.UltimoProveedorId, factura.ProveedorId), ct);

            _context.ArticuloHistorialCompra.Add(new ArticuloHistorialCompra
            {
                ArticuloId = linea.ArticuloId,
                ProveedorId = factura.ProveedorId,
                FacturaCompraId = fila.Id,
                PrecioCosto = linea.CostoUnitario,
                Fecha = factura.FechaRegistro,
                Anulado = false
            });
        }
        await _context.SaveChangesAsync(ct);

        return Result<int>.Success(fila.Id);
    }

    public async Task<(IReadOnlyList<FacturaCompraResumen> Items, int Total)> BuscarAsync(
        FiltroFacturasCompra filtro, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var consulta = _context.FacturaCompra.AsNoTracking();
        if (filtro.ProveedorId is { } proveedor) consulta = consulta.Where(f => f.ProveedorId == proveedor);
        if (filtro.Numero is { } numero) consulta = consulta.Where(f => f.Numero == numero);
        if (filtro.EmisionDesde is { } emisionDesde) consulta = consulta.Where(f => f.FechaEmision >= emisionDesde);
        if (filtro.EmisionHasta is { } emisionHasta) consulta = consulta.Where(f => f.FechaEmision <= emisionHasta);
        if (filtro.VencimientoDesde is { } vencimientoDesde) consulta = consulta.Where(f => f.FechaVencimiento >= vencimientoDesde);
        if (filtro.VencimientoHasta is { } vencimientoHasta) consulta = consulta.Where(f => f.FechaVencimiento <= vencimientoHasta);

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(f => f.FechaEmision).ThenByDescending(f => f.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(f => new FacturaCompraResumen(f.Id, f.ProveedorId, f.Proveedor.RazonSocial, f.Numero,
                f.TipoComprobante.HasValue ? f.TipoComprobante.Value.ToString() : "", f.FechaEmision, f.FechaVencimiento,
                f.Estado != EstadoActiva))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<FacturaCompraParaAnular?> ObtenerParaAnularAsync(int id, CancellationToken ct = default)
        => await _context.FacturaCompra.AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new FacturaCompraParaAnular(f.Id, f.Numero, f.Estado == EstadoActiva,
                f.FacturaCompraDetalle.Select(d => d.ArticuloId).Distinct().ToList()))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Como el registro, varias sentencias que son atómicas solo porque el caso de uso las
    /// ejecuta dentro de IUnidadDeTrabajo.
    /// </summary>
    public async Task<Result> AnularConEfectosAsync(AnulacionFacturaCompra anulacion, CancellationToken ct = default)
    {
        // La cabecera primero y solo si sigue ACTIVA: dos anulaciones simultáneas no restan dos veces.
        var marcadas = await _context.FacturaCompra
            .Where(f => f.Id == anulacion.FacturaId && f.Estado == EstadoActiva)
            .ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Estado, EstadoAnulada)
                .SetProperty(f => f.MotivoAnulacion, anulacion.Motivo)
                .SetProperty(f => f.FechaAnulacion, anulacion.Fecha)
                .SetProperty(f => f.UsuarioAnulacionId, anulacion.UsuarioId)
                .SetProperty(f => f.UsuarioAutorizacionId, anulacion.UsuarioAutorizacionId), ct);
        if (marcadas == 0)
            return Result.Failure(TipoError.Conflicto, "La factura ya no está activa.");

        var lineas = await _context.FacturaCompraDetalle.AsNoTracking()
            .Where(d => d.FacturaCompraId == anulacion.FacturaId)
            .OrderBy(d => d.ArticuloId).ThenBy(d => d.SucursalId)
            .Select(d => new { d.ArticuloId, d.SucursalId, d.Cantidad })
            .ToListAsync(ct);
        foreach (var linea in lineas)
        {
            // CA-003: resta aunque quede negativo. Si no entra en stock.cantidad, 0 filas: Invalido.
            var filas = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE stock SET cantidad = cantidad - {linea.Cantidad}
                WHERE articulo_id = {linea.ArticuloId} AND sucursal_id = {linea.SucursalId}
                  AND cantidad - {linea.Cantidad} > {-LimiteStock}
                """, ct);
            if (filas == 0)
                return Result.Failure(TipoError.Invalido,
                    $"El stock del artículo {linea.ArticuloId} en la sucursal {linea.SucursalId} no se puede revertir.");
        }

        await _context.ArticuloHistorialCompra
            .Where(h => h.FacturaCompraId == anulacion.FacturaId)
            .ExecuteUpdateAsync(s => s.SetProperty(h => h.Anulado, true), ct);

        // CA-001 y CA-002: cada artículo vuelve a su último registro válido. Sin ninguno, queda
        // sin último proveedor y conserva el costo que tiene.
        foreach (var articuloId in lineas.Select(l => l.ArticuloId).Distinct())
        {
            var anterior = await _context.ArticuloHistorialCompra.AsNoTracking()
                .Where(h => h.ArticuloId == articuloId && !h.Anulado)
                .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
                .Select(h => new { h.PrecioCosto, h.ProveedorId })
                .FirstOrDefaultAsync(ct);
            var articulo = _context.Articulo.Where(a => a.Id == articuloId);
            if (anterior is null)
                await articulo.ExecuteUpdateAsync(s => s.SetProperty(a => a.UltimoProveedorId, (int?)null), ct);
            else
                await articulo.ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.PrecioCosto, anterior.PrecioCosto)
                    .SetProperty(a => a.UltimoProveedorId, anterior.ProveedorId), ct);
        }

        return Result.Success();
    }

    public async Task<IReadOnlyDictionary<string, int>> ObtenerArticulosPorCodigoProveedorAsync(
        int proveedorId, IReadOnlyCollection<string> codigos, CancellationToken ct = default)
        => await _context.ArticuloCodigoAlternativo.AsNoTracking()
            .Where(c => c.ProveedorId == proveedorId && codigos.Contains(c.Codigo) && c.Articulo.Activo)
            .ToDictionaryAsync(c => c.Codigo, c => c.ArticuloId, ct);
}

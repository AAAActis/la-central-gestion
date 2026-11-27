using LaCentral.Data.Models;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

/// <summary>HU-FCO-07 (057) · Nota de crédito de proveedor.</summary>
public class NotaCreditoCompraRepositorio : INotaCreditoCompraRepositorio
{
    // Valores de los CHECK ncc_estado_valido y ncc_origen_valido (y fc_estado_valido).
    private const string EstadoActiva = "ACTIVA";
    private const string EstadoAnulada = "ANULADA";
    private const string OrigenManual = "MANUAL";
    private const string OrigenPdf = "PDF";
    // stock.cantidad es NUMERIC(12,3): la parte entera tiene a lo sumo 9 dígitos.
    private const decimal LimiteStock = 1_000_000_000m;

    private readonly LaCentralDbContext _context;

    public NotaCreditoCompraRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<bool> ExisteNumeroAsync(int proveedorId, string numero, CancellationToken ct = default)
        => await _context.NotaCreditoCompra.AnyAsync(n => n.ProveedorId == proveedorId && n.Numero == numero && n.Estado == EstadoActiva, ct);

    public async Task<IReadOnlyList<FacturaParaNotaCredito>> ObtenerFacturasAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
        => await _context.FacturaCompra.AsNoTracking()
            .Where(f => ids.Contains(f.Id))
            .Select(f => new FacturaParaNotaCredito(f.Id, f.ProveedorId, f.Estado == EstadoActiva))
            .ToListAsync(ct);

    private sealed record FacturaBloqueada(int Id, string Numero, string Estado);

    /// <summary>
    /// FOR UPDATE sobre las facturas, en orden de Id (dos transacciones no se cruzan). Lo toman el
    /// registro de una nota y la anulación de la factura: una nota y la anulación de su factura no
    /// pasan juntas, ni dos devoluciones sobre la misma factura. Lo que se lea después, en otra
    /// sentencia, ya ve lo que confirmó quien tenía el bloqueo.
    /// </summary>
    private async Task<List<FacturaBloqueada>> BloquearFacturasAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
        => await _context.Database.SqlQuery<FacturaBloqueada>($"""
            SELECT id AS "Id", numero AS "Numero", estado AS "Estado" FROM factura_compra
            WHERE id = ANY({ids.ToArray()}) ORDER BY id FOR UPDATE
            """).ToListAsync(ct);

    public async Task<IReadOnlyList<CompradoYDevuelto>> ObtenerCompradoYDevueltoAsync(IReadOnlyCollection<int> facturaIds, CancellationToken ct = default)
    {
        await BloquearFacturasAsync(facturaIds, ct);
        var comprado = await _context.FacturaCompraDetalle.AsNoTracking()
            .Where(d => facturaIds.Contains(d.FacturaCompraId))
            .GroupBy(d => d.ArticuloId)
            .Select(g => new { ArticuloId = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
            .ToListAsync(ct);
        var devuelto = await _context.NotaCreditoCompraDetalle.AsNoTracking()
            .Where(d => d.NotaCreditoCompra.Estado == EstadoActiva
                && d.NotaCreditoCompra.NotaCreditoCompraFactura.Any(f => facturaIds.Contains(f.FacturaCompraId)))
            .GroupBy(d => d.ArticuloId)
            .Select(g => new { ArticuloId = g.Key, Cantidad = g.Sum(d => d.Cantidad) })
            .ToDictionaryAsync(g => g.ArticuloId, g => g.Cantidad, ct);
        return comprado.Select(c => new CompradoYDevuelto(c.ArticuloId, c.Cantidad, devuelto.GetValueOrDefault(c.ArticuloId))).ToList();
    }

    /// <summary>
    /// Varias sentencias que son atómicas solo porque el caso de uso las ejecuta dentro de
    /// IUnidadDeTrabajo. No toca costo, precios ni historial de compra.
    /// </summary>
    public async Task<Result<int>> RegistrarConEfectosAsync(LaCentral.UseCases.Entidades.NotaCreditoCompra nota, CancellationToken ct = default)
    {
        // La validación previa del caso de uso corrió sin bloqueo: si una factura se anuló entre medio, no entra.
        if ((await BloquearFacturasAsync(nota.FacturaIds, ct)).FirstOrDefault(f => f.Estado != EstadoActiva) is { } anulada)
            return Result<int>.Failure(TipoError.Conflicto, $"La factura {anulada.Numero} ya no está activa: no se le puede asociar la nota de crédito.");

        var fila = new NotaCreditoCompra
        {
            ProveedorId = nota.ProveedorId,
            Numero = nota.Numero,
            FechaEmision = nota.FechaEmision,
            Clase = nota.Clase,
            ImporteNeto = nota.ImporteNeto,
            PercepcionIva = nota.PercepcionIva,
            PercepcionIibb = nota.PercepcionIibb,
            PercepcionMunicipal = nota.PercepcionMunicipal,
            PercepcionGanancias = nota.PercepcionGanancias,
            Origen = nota.DesdePdf ? OrigenPdf : OrigenManual,
            UsuarioId = nota.UsuarioId,
            FechaRegistro = nota.FechaRegistro,
            Estado = EstadoActiva,
            NotaCreditoCompraFactura = nota.FacturaIds.Select(id => new NotaCreditoCompraFactura
            {
                FacturaCompraId = id, ProveedorId = nota.ProveedorId
            }).ToList(),
            NotaCreditoCompraDetalle = nota.Lineas.Select(l => new NotaCreditoCompraDetalle
            {
                ArticuloId = l.ArticuloId, SucursalId = l.SucursalId, Cantidad = l.Cantidad
            }).ToList()
        };
        _context.NotaCreditoCompra.Add(fila);
        await _context.SaveChangesAsync(ct);

        foreach (var linea in nota.Lineas.OrderBy(l => l.ArticuloId).ThenBy(l => l.SucursalId))
        {
            // Resta en el origen aunque quede negativo (D-S6-24); si no había fila (Reclamos), la crea
            // en negativo. Si no entra en stock.cantidad, 0 filas: Invalido y la unidad de trabajo descarta todo.
            var filas = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO stock (articulo_id, sucursal_id, cantidad)
                VALUES ({linea.ArticuloId}, {linea.SucursalId}, {-linea.Cantidad})
                ON CONFLICT (articulo_id, sucursal_id)
                DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad
                WHERE stock.cantidad + EXCLUDED.cantidad > {-LimiteStock}
                """, ct);
            if (filas == 0)
                return Result<int>.Failure(TipoError.Invalido,
                    $"El stock del artículo {linea.ArticuloId} en la sucursal {linea.SucursalId} quedaría por debajo del mínimo que se puede registrar.");
        }
        return Result<int>.Success(fila.Id);
    }

    public async Task<IReadOnlyList<string>> ObtenerVigentesDeFacturaAsync(int facturaId, CancellationToken ct = default)
    {
        await BloquearFacturasAsync([facturaId], ct);
        return await _context.NotaCreditoCompra.AsNoTracking()
            .Where(n => n.Estado == EstadoActiva && n.NotaCreditoCompraFactura.Any(f => f.FacturaCompraId == facturaId))
            .OrderBy(n => n.Numero)
            .Select(n => n.Numero)
            .ToListAsync(ct);
    }

    public async Task<NotaCreditoParaAnular?> ObtenerParaAnularAsync(int id, CancellationToken ct = default)
        => await _context.NotaCreditoCompra.AsNoTracking()
            .Where(n => n.Id == id)
            .Select(n => new NotaCreditoParaAnular(n.Id, n.Numero, n.Estado == EstadoActiva))
            .FirstOrDefaultAsync(ct);

    /// <summary>Como el registro, atómica solo dentro de IUnidadDeTrabajo.</summary>
    public async Task<Result> AnularConEfectosAsync(AnulacionNotaCreditoCompra anulacion, CancellationToken ct = default)
    {
        // La cabecera primero y solo si sigue ACTIVA: dos anulaciones simultáneas no devuelven dos veces.
        var marcadas = await _context.NotaCreditoCompra
            .Where(n => n.Id == anulacion.NotaId && n.Estado == EstadoActiva)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.Estado, EstadoAnulada)
                .SetProperty(n => n.MotivoAnulacion, anulacion.Motivo)
                .SetProperty(n => n.FechaAnulacion, anulacion.Fecha)
                .SetProperty(n => n.UsuarioAnulacionId, anulacion.UsuarioId), ct);
        if (marcadas == 0)
            return Result.Failure(TipoError.Conflicto, "La nota de crédito ya no está activa.");

        var lineas = await _context.NotaCreditoCompraDetalle.AsNoTracking()
            .Where(d => d.NotaCreditoCompraId == anulacion.NotaId)
            .OrderBy(d => d.ArticuloId).ThenBy(d => d.SucursalId)
            .Select(d => new { d.ArticuloId, d.SucursalId, d.Cantidad })
            .ToListAsync(ct);
        foreach (var linea in lineas)
        {
            // La fila existe: la escribió el registro. Si la suma no entra en stock.cantidad, Invalido.
            var filas = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE stock SET cantidad = cantidad + {linea.Cantidad}
                WHERE articulo_id = {linea.ArticuloId} AND sucursal_id = {linea.SucursalId}
                  AND cantidad + {linea.Cantidad} < {LimiteStock}
                """, ct);
            if (filas == 0)
                return Result.Failure(TipoError.Invalido,
                    $"El stock del artículo {linea.ArticuloId} en la sucursal {linea.SucursalId} superaría el máximo que se puede registrar.");
        }
        return Result.Success();
    }

    public async Task<(IReadOnlyList<NotaCreditoCompraResumen> Items, int Total)> BuscarAsync(
        FiltroNotasCreditoCompra filtro, int pagina, int tamanoPagina, CancellationToken ct = default)
    {
        var consulta = _context.NotaCreditoCompra.AsNoTracking();
        if (filtro.ProveedorId is { } proveedor) consulta = consulta.Where(n => n.ProveedorId == proveedor);
        if (filtro.EmisionDesde is { } desde) consulta = consulta.Where(n => n.FechaEmision >= desde);
        if (filtro.EmisionHasta is { } hasta) consulta = consulta.Where(n => n.FechaEmision <= hasta);

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(n => n.FechaEmision).ThenByDescending(n => n.Id)
            .Skip((pagina - 1) * tamanoPagina).Take(tamanoPagina)
            .Select(n => new NotaCreditoCompraResumen(n.Id, n.ProveedorId, n.Proveedor.RazonSocial, n.Numero, n.Clase,
                n.FechaEmision, n.ImporteNeto, n.Estado != EstadoActiva))
            .ToListAsync(ct);
        return (items, total);
    }
}

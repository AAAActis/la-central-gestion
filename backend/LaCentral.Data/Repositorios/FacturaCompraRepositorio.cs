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
    private const string OrigenManual = "MANUAL";

    private readonly LaCentralDbContext _context;

    public FacturaCompraRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<FacturaCompraExistente?> ObtenerNumeroActivoAsync(int proveedorId, string numero, CancellationToken ct = default)
        => await _context.FacturaCompra.AsNoTracking()
            .Where(f => f.ProveedorId == proveedorId && f.Numero == numero && f.Estado == EstadoActiva)
            .Select(f => new FacturaCompraExistente(f.Id, f.Numero, f.FechaEmision))
            .FirstOrDefaultAsync(ct);

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
            PercepcionIva = factura.PercepcionIva,
            PercepcionIibb = factura.PercepcionIibb,
            PercepcionMunicipal = factura.PercepcionMunicipal,
            PercepcionGanancias = factura.PercepcionGanancias,
            Estado = EstadoActiva,
            Origen = OrigenManual,
            UsuarioId = factura.UsuarioId,
            FechaRegistro = factura.FechaRegistro,
            FacturaCompraDetalle = factura.Lineas.Select(l => new FacturaCompraDetalle
            {
                ArticuloId = l.ArticuloId,
                SucursalId = l.SucursalId,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                DescuentoPorcentaje = l.DescuentoPorcentaje
            }).ToList()
        };
        _context.FacturaCompra.Add(fila);
        await _context.SaveChangesAsync(ct);

        foreach (var linea in factura.Lineas)
        {
            // D9: suma en el destino de la línea. El incremento lo hace el motor, así dos
            // compras simultáneas del mismo artículo no se pisan; si no había fila, se crea.
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO stock (articulo_id, sucursal_id, cantidad)
                VALUES ({linea.ArticuloId}, {linea.SucursalId}, {linea.Cantidad})
                ON CONFLICT (articulo_id, sucursal_id)
                DO UPDATE SET cantidad = stock.cantidad + EXCLUDED.cantidad
                """, ct);

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
}

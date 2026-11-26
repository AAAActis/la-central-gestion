using LaCentral.Data.Models;
using LaCentral.UseCases.Comparador;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

/// <summary>030 · El cruce de articulo_codigo_alternativo con proveedor_articulo por (proveedor_id, código).</summary>
public class ComparadorRepositorio : IComparadorRepositorio
{
    private readonly LaCentralDbContext _context;
    public ComparadorRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<DatosComparador?> ObtenerAsync(int articuloId, CancellationToken ct = default)
    {
        var articulo = await _context.Articulo.AsNoTracking().Where(a => a.Id == articuloId)
            .Select(a => new { a.UltimoProveedorId }).SingleOrDefaultAsync(ct);
        if (articulo is null) return null;

        // D-S7-14: el código de la ficha está sin espacios en los extremos y en mayúsculas; el del scrape, como viene.
        var filas = await (
                from c in _context.ArticuloCodigoAlternativo.AsNoTracking()
                join pa in _context.ProveedorArticulo.AsNoTracking()
                    on new { c.ProveedorId, c.Codigo } equals new { pa.ProveedorId, Codigo = pa.Codigo.Trim(new[] { ' ' }).ToUpper() }
                where c.ArticuloId == articuloId && pa.Proveedor.Activo && !pa.Vencido
                select new FilaCatalogoVinculada(pa.ProveedorId, pa.Proveedor.RazonSocial, pa.Proveedor.UrlReferencia,
                    pa.Codigo, pa.Nombre, pa.Marca, pa.PrecioNeto, pa.PrecioLista, pa.EstadoStock, pa.Stock, pa.UrlImagen, pa.UltimaLectura))
            .ToListAsync(ct);
        return new DatosComparador(articulo.UltimoProveedorId, filas);
    }
}

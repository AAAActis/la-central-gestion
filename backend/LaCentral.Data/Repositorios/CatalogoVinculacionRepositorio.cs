using LaCentral.Data.Models;
using LaCentral.UseCases.Catalogo;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaCentral.Data.Repositorios;

/// <summary>
/// 054a · Vinculación en SQL: usuario_id y fecha_vinculacion son del script 16 y la unicidad que
/// decide la carrera es codigo_alt_proveedor_codigo_unico (proveedor_id, codigo).
/// </summary>
public class CatalogoVinculacionRepositorio : ICatalogoVinculacionRepositorio
{
    private readonly LaCentralDbContext _context;
    public CatalogoVinculacionRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<FilaParaVincularDto?> ObtenerFilaAsync(int filaId, CancellationToken ct = default)
        => (await _context.Database.SqlQueryRaw<Fila>("""
                SELECT pa.id AS "Id", pa.proveedor_id AS "ProveedorId", pa.codigo AS "Codigo",
                       pa.vencido AS "Vencido", p.activo AS "ProveedorActivo"
                  FROM proveedor_articulo pa JOIN proveedor p ON p.id = pa.proveedor_id
                 WHERE pa.id = @fila
                """, new NpgsqlParameter("fila", filaId)).ToListAsync(ct))
            .Select(f => new FilaParaVincularDto(f.Id, f.ProveedorId, f.Codigo, f.Vencido, f.ProveedorActivo))
            .SingleOrDefault();

    public async Task<bool?> ArticuloActivoAsync(int articuloId, CancellationToken ct = default)
        => await _context.Articulo.AsNoTracking().Where(a => a.Id == articuloId)
            .Select(a => (bool?)a.Activo).SingleOrDefaultAsync(ct);

    public async Task<ArticuloVinculadoDto?> VinculadoAAsync(int proveedorId, string codigo, CancellationToken ct = default)
        => await _context.ArticuloCodigoAlternativo.AsNoTracking()
            .Where(c => c.ProveedorId == proveedorId && c.Codigo == codigo)
            .Select(c => new ArticuloVinculadoDto(c.Articulo.Id, c.Articulo.CodigoInterno, c.Articulo.Nombre))
            .SingleOrDefaultAsync(ct);

    // CA1: el código, el usuario y la fecha son una sola fila, así que van juntos o no va ninguno.
    public async Task<bool> VincularAsync(int articuloId, int proveedorId, string codigo, int usuarioId, CancellationToken ct = default)
        => await _context.Database.ExecuteSqlRawAsync("""
                INSERT INTO articulo_codigo_alternativo (articulo_id, proveedor_id, codigo, usuario_id, fecha_vinculacion)
                VALUES (@articulo, @proveedor, @codigo, @usuario, now())
                ON CONFLICT ON CONSTRAINT codigo_alt_proveedor_codigo_unico DO NOTHING
                """, [new NpgsqlParameter("articulo", articuloId), new NpgsqlParameter("proveedor", proveedorId),
                    new NpgsqlParameter("codigo", codigo), new NpgsqlParameter("usuario", usuarioId)], ct) == 1;

    private sealed class Fila
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public string Codigo { get; set; } = null!;
        public bool Vencido { get; set; }
        public bool ProveedorActivo { get; set; }
    }
}

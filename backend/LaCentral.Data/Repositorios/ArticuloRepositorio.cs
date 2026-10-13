using LaCentral.Data.Models;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

public class ArticuloRepositorio : IConsultaArticulosRepositorio
{
    private readonly LaCentralDbContext _context;

    public ArticuloRepositorio(LaCentralDbContext context) => _context = context;

    // Query completo antes de paginar, expuesto a los tests de traducción SQL.
    internal IQueryable<CoincidenciaArticulo> CrearCoincidencias(ConsultaArticulos consulta)
    {
        consulta.Validar();
        var articulos = _context.Articulo.AsNoTracking();
        if (!consulta.IncluirInactivos)
            articulos = articulos.Where(a => a.Activo);

        IQueryable<CoincidenciaArticulo>? coincidencias = null;
        foreach (var alternativa in NormalizacionArticulo.Alternativas(consulta.Texto))
        {
            var interno = articulos.Where(a => a.CodigoInterno.Trim(new[] { ' ' }).ToUpper() == alternativa)
                .Select(a => new CoincidenciaArticulo { Id = a.Id, Prioridad = 0 });
            var proveedor = articulos.Where(a => a.ArticuloCodigoAlternativo.Any(
                    c => c.Codigo.Trim(new[] { ' ' }).ToUpper() == alternativa))
                .Select(a => new CoincidenciaArticulo { Id = a.Id, Prioridad = 1 });

            var nombres = articulos;
            foreach (var palabra in NormalizacionArticulo.Palabras(alternativa))
            {
                var termino = palabra;
                nombres = nombres.Where(a => a.Nombre.ToUpper()
                    .Replace("Á", "A").Replace("É", "E").Replace("Í", "I")
                    .Replace("Ó", "O").Replace("Ú", "U").Replace("Ü", "U").Contains(termino));
            }

            var grupo = interno.Concat(proveedor).Concat(nombres.Select(
                a => new CoincidenciaArticulo { Id = a.Id, Prioridad = 2 }));
            coincidencias = coincidencias is null ? grupo : coincidencias.Concat(grupo);
        }

        return coincidencias!.GroupBy(c => c.Id)
            .Select(g => new CoincidenciaArticulo { Id = g.Key, Prioridad = g.Min(c => c.Prioridad) });
    }

    internal IQueryable<ArticuloResumenConsultaDto> CrearPagina(ConsultaArticulos consulta)
    {
        var coincidencias = CrearCoincidencias(consulta);
        return (from c in coincidencias
                join a in _context.Articulo.AsNoTracking() on c.Id equals a.Id
                orderby c.Prioridad, a.Nombre, a.Id
                select new ArticuloResumenConsultaDto(a.Id, a.CodigoInterno, a.Nombre, a.Activo))
            .Skip((consulta.Pagina - 1) * consulta.TamanoPagina).Take(consulta.TamanoPagina);
    }

    public async Task<PaginaArticulosDto> BuscarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
    {
        var total = await CrearCoincidencias(consulta).CountAsync(ct);
        var resultados = await CrearPagina(consulta).ToListAsync(ct);
        var conflictos = new List<ConflictoCodigoArticuloDto>();
        foreach (var alternativa in NormalizacionArticulo.Alternativas(consulta.Texto))
        {
            // Incluye conflictos fuera de esta página y artículos inactivos.
            var exactos = await ObtenerConflictosCodigoAsync(alternativa, ct);
            if (exactos.Count > 1)
                conflictos.Add(new(alternativa, exactos.Select(a => a.Id).ToArray()));
        }
        return new(resultados, total, consulta.Pagina, consulta.TamanoPagina, conflictos);
    }

    public async Task<IReadOnlyList<ArticuloResumenConsultaDto>> ObtenerConflictosCodigoAsync(
        string codigo, CancellationToken ct = default)
    {
        var clave = NormalizacionArticulo.Codigo(codigo);
        if (clave.Length == 0) throw new ArgumentException("El código no puede estar vacío.", nameof(codigo));
        return await _context.Articulo.AsNoTracking()
            .Where(a => a.CodigoInterno.Trim(new[] { ' ' }).ToUpper() == clave ||
                a.ArticuloCodigoAlternativo.Any(c => c.Codigo.Trim(new[] { ' ' }).ToUpper() == clave))
            .OrderBy(a => a.Id)
            .Select(a => new ArticuloResumenConsultaDto(a.Id, a.CodigoInterno, a.Nombre, a.Activo))
            .ToListAsync(ct);
    }

    public async Task<ArticuloDetalleConsultaDto?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
    {
        var articulo = await _context.Articulo.AsNoTracking()
            .Include(a => a.ArticuloCodigoAlternativo).ThenInclude(c => c.Proveedor)
            .Include(a => a.Stock).ThenInclude(s => s.Sucursal)
            .SingleOrDefaultAsync(a => a.Id == id, ct);
        if (articulo is null) return null;

        return new(articulo.Id, articulo.CodigoInterno, articulo.Nombre, articulo.Activo,
            articulo.PrecioCosto, articulo.UltimoProveedorId,
            articulo.ArticuloCodigoAlternativo.OrderBy(c => c.ProveedorId).ThenBy(c => c.Id)
                .Select(c => new CodigoProveedorConsultaDto(c.ProveedorId, c.Proveedor.RazonSocial,
                    c.Codigo, NormalizacionArticulo.Codigo(c.Codigo))).ToArray(),
            articulo.Stock.OrderBy(s => s.SucursalId)
                .Select(s => new StockSucursalConsultaDto(s.SucursalId, s.Sucursal.Nombre, s.Cantidad)).ToArray(),
            // D1: el esquema vigente no identifica modalidades. No interpretar
            // márgenes legados ni calcular una fórmula que el equipo retiró.
            PreciosPorModalidad: null);
    }

    internal sealed class CoincidenciaArticulo
    {
        public int Id { get; set; }
        public int Prioridad { get; set; }
    }
}

using LaCentral.Data.Models;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Repositorios;

public class ArticuloRepositorio : IConsultaArticulosRepositorio, IArticuloRepositorio, IPreciosArticuloRepositorio
{
    private readonly LaCentralDbContext _context;

    public ArticuloRepositorio(LaCentralDbContext context) => _context = context;

    public async Task<IReadOnlyDictionary<int, PreciosDeArticulo>> ObtenerAsync(IReadOnlyCollection<int> articuloIds, CancellationToken ct = default)
    {
        var articulos = await _context.Articulo.AsNoTracking()
            .Where(a => articuloIds.Contains(a.Id))
            .Select(a => new { a.Id, a.PrecioCosto, Filas = a.ArticuloPrecioModalidad.ToList() })
            .ToListAsync(ct);

        return articulos.ToDictionary(a => a.Id, a => new PreciosDeArticulo(a.PrecioCosto, a.Filas
            .Select(p => new LaCentral.UseCases.Entidades.PrecioModalidad
            {
                Modalidad = ModalidadPersistida.Leer(p.Modalidad),
                Base = ModalidadPersistida.LeerBase(p.Base),
                Porcentaje = p.Porcentaje,
                Precio = p.Precio,
                CalculoManual = p.CalculoManual,
                FechaCalculo = p.FechaCalculo,
                UsuarioId = p.UsuarioId
            }).ToList()));
    }

    /// <summary>
    /// Por artículo y modalidad (la clave primaria). Solo precio y fecha: la configuración
    /// (base, porcentaje, calculo_manual) y el usuario no los cambia un recálculo. Nunca
    /// escribe una fila manual: el filtro va en el UPDATE, así que vale lo que la fila es al
    /// escribir, no lo que era al leer (otra sesión pudo fijarla entre medio).
    /// </summary>
    public async Task ActualizarPreciosAsync(int articuloId, IReadOnlyList<LaCentral.UseCases.Entidades.PrecioModalidad> precios, CancellationToken ct = default)
    {
        foreach (var precio in precios)
        {
            var modalidad = ModalidadPersistida.Escribir(precio.Modalidad);
            await _context.ArticuloPrecioModalidad
                .Where(p => p.ArticuloId == articuloId && p.Modalidad == modalidad && !p.CalculoManual)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Precio, precio.Precio)
                    .SetProperty(p => p.FechaCalculo, precio.FechaCalculo), ct);
        }
    }

    /// <summary>
    /// Un solo SaveChanges para artículo, códigos, stock y precios. codigo_interno es NOT NULL
    /// y único: entra un provisional irrepetible hasta que el caso de uso asigna el definitivo,
    /// derivado del Id, en la misma transacción.
    /// </summary>
    public async Task<int> AgregarAsync(LaCentral.UseCases.Entidades.Articulo articulo, CancellationToken ct = default)
    {
        var fila = new Articulo
        {
            CodigoInterno = "TMP-" + Guid.NewGuid().ToString("N"),
            Nombre = articulo.Nombre,
            UbicacionDeposito = articulo.UbicacionDeposito,
            PrecioCosto = articulo.CostoVigente,
            Activo = articulo.Activo,
            MotivoBaja = articulo.MotivoBaja,
            FechaBaja = articulo.FechaBaja,
            ArticuloCodigoAlternativo = articulo.CodigosAlternativos
                .Select(c => new ArticuloCodigoAlternativo { Codigo = c.Codigo, ProveedorId = c.ProveedorId }).ToList(),
            Stock = articulo.Existencias
                .Select(e => new Stock { SucursalId = checked((short)e.SucursalId), Cantidad = e.Cantidad }).ToList(),
            ArticuloPrecioModalidad = articulo.Precios.Select(p => new ArticuloPrecioModalidad
            {
                Modalidad = ModalidadPersistida.Escribir(p.Modalidad),
                Base = ModalidadPersistida.EscribirBase(p.Base),
                Porcentaje = p.Porcentaje,
                Precio = p.Precio,
                CalculoManual = p.CalculoManual,
                FechaCalculo = p.FechaCalculo,
                UsuarioId = p.UsuarioId
            }).ToList()
        };
        _context.Articulo.Add(fila);
        await _context.SaveChangesAsync(ct);
        return fila.Id;
    }

    public async Task<bool> AsignarCodigoInternoAsync(int id, string codigoInterno, CancellationToken ct = default)
        => await _context.Articulo.Where(a => a.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.CodigoInterno, codigoInterno), ct) == 1;

    public async Task<IReadOnlyList<short>> ObtenerSucursalesAsync(CancellationToken ct = default)
        => await _context.Sucursal.AsNoTracking().OrderBy(s => s.Id).Select(s => s.Id).ToListAsync(ct);

    // Query completo antes de paginar, expuesto a los tests de traducción SQL.
    internal IQueryable<CoincidenciaArticulo> CrearCoincidencias(ConsultaArticulos consulta)
    {
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
        // Los códigos de proveedor de la página, en una sola consulta aparte: la paginación
        // se hace sobre artículos y no se multiplica por sus códigos.
        var ids = resultados.Select(a => a.Id).ToArray();
        var codigos = (await _context.ArticuloCodigoAlternativo.AsNoTracking()
                .Where(c => ids.Contains(c.ArticuloId))
                .OrderBy(c => c.ProveedorId).ThenBy(c => c.Id)
                .Select(c => new { c.ArticuloId, Codigo = new CodigoProveedorResumenDto(c.ProveedorId, c.Proveedor.RazonSocial, c.Codigo) })
                .ToListAsync(ct))
            .ToLookup(c => c.ArticuloId, c => c.Codigo);
        var articulos = resultados
            .Select(a => new ArticuloResultadoBusquedaDto(a.Id, a.CodigoInterno, a.Nombre, a.Activo,
                codigos[a.Id].ToArray())).ToArray();
        return new(articulos, total, consulta.Pagina, consulta.TamanoPagina, conflictos);
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
            .Include(a => a.ArticuloPrecioModalidad)
            .SingleOrDefaultAsync(a => a.Id == id, ct);
        if (articulo is null) return null;

        return new(articulo.Id, articulo.CodigoInterno, articulo.Nombre, articulo.Activo,
            articulo.PrecioCosto, articulo.UltimoProveedorId,
            articulo.ArticuloCodigoAlternativo.OrderBy(c => c.ProveedorId).ThenBy(c => c.Id)
                .Select(c => new CodigoProveedorConsultaDto(c.ProveedorId, c.Proveedor.RazonSocial,
                    c.Codigo, NormalizacionArticulo.Codigo(c.Codigo))).ToArray(),
            articulo.Stock.OrderBy(s => s.SucursalId)
                .Select(s => new StockSucursalConsultaDto(s.SucursalId, s.Sucursal.Nombre, s.Cantidad)).ToArray(),
            // D1: precios estampados en articulo_precio_modalidad, sin recalcular.
            // Sin filas, lista vacía: la razón de cada ausencia la arma ObtenerArticuloDetalleUseCase.
            PreciosPorModalidad: articulo.ArticuloPrecioModalidad
                    .Select(p => new PrecioModalidadConsultaDto(ModalidadPersistida.Leer(p.Modalidad),
                        ModalidadPersistida.LeerBase(p.Base), p.Porcentaje, p.Precio,
                        p.CalculoManual, p.FechaCalculo, p.UsuarioId))
                    .OrderBy(p => p.Modalidad).ToArray());
    }

    internal sealed class CoincidenciaArticulo
    {
        public int Id { get; set; }
        public int Prioridad { get; set; }
    }
}

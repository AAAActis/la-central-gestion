using LaCentral.Data.Models;
using LaCentral.UseCases.Catalogo;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaCentral.Data.Repositorios;

/// <summary>053 · Candidatos sobre proveedor_articulo, en SQL: la similitud es pg_trgm.</summary>
public class CatalogoCandidatosRepositorio : ICatalogoCandidatosRepositorio
{
    private readonly LaCentralDbContext _context;
    public CatalogoCandidatosRepositorio(LaCentralDbContext context) => _context = context;

    /// <summary>La misma regla que NormalizacionCatalogo.Clave, en SQL. Las dos cajas: upper() no pasa a mayúscula fuera de ASCII en todo locale.</summary>
    private static string Normalizar(string columna) =>
        $"regexp_replace(translate(upper({columna}), 'ÁÉÍÓÚÜÑáéíóúüñ', 'AEIOUUNAEIOUUN'), '[^A-Z0-9]', '', 'g')";

    // D-S7-08 y D-S7-14: vinculada si (proveedor_id, código) está en articulo_codigo_alternativo, con el
    // código como lo guarda la ficha. CA3: proveedor activo. D-S7-10: sin vencidos.
    private const string Desde = """
          FROM proveedor_articulo pa
          JOIN proveedor p ON p.id = pa.proveedor_id AND p.activo
          LEFT JOIN articulo_codigo_alternativo aca
                 ON aca.proveedor_id = pa.proveedor_id AND aca.codigo = upper(btrim(pa.codigo))
          LEFT JOIN articulo a ON a.id = aca.articulo_id
         WHERE NOT pa.vencido
        """;

    private const string Columnas = """
        SELECT pa.id AS "Id", pa.proveedor_id AS "ProveedorId", p.razon_social AS "Proveedor", pa.codigo AS "Codigo",
               pa.nombre AS "Nombre", pa.numero_original AS "NumeroOriginal", pa.marca AS "Marca",
               a.id AS "ArticuloId", a.codigo_interno AS "ArticuloCodigoInterno", a.nombre AS "ArticuloNombre"
        """;

    // Solo se interpolan constantes de esta clase; los valores van siempre como parámetros.
    private static readonly string SqlTextos = $"""
            SELECT pa.nombre AS "Value" {Desde} AND aca.articulo_id = @articulo ORDER BY pa.id
            """;

    private static readonly string SqlExactas = $"""
            {Columnas}, NULL::float8 AS "Parecido"
            {Desde}
               AND @clave IN ({Normalizar("pa.codigo")}, {Normalizar("pa.numero_original")},
                              {Normalizar("pa.codigo_barras")}, {Normalizar("pa.nombre")})
             ORDER BY p.razon_social, pa.codigo, pa.id
             LIMIT 200
            """;

    private static readonly string SqlAproximadas = $"""
            SELECT * FROM (
                {Columnas},
                       (SELECT max(greatest(similarity(pa.nombre, t), coalesce(similarity(pa.numero_original, t), 0)))
                          FROM unnest(@textos) AS t)::float8 AS "Parecido"
                {Desde}
            ) c
             WHERE c."Parecido" >= @umbral
             ORDER BY c."Parecido" DESC, c."Id"
             LIMIT @limite
            """;

    public async Task<IReadOnlyList<string>?> TextosDeArticuloAsync(int articuloId, CancellationToken ct = default)
    {
        var nombre = await _context.Articulo.AsNoTracking().Where(a => a.Id == articuloId)
            .Select(a => a.Nombre).SingleOrDefaultAsync(ct);
        if (nombre is null) return null;
        var vinculadas = await _context.Database.SqlQueryRaw<string>(SqlTextos, new NpgsqlParameter("articulo", articuloId)).ToListAsync(ct);
        return [nombre, .. vinculadas];
    }

    public async Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarExactasAsync(string clave, CancellationToken ct = default)
    {
        var filas = await _context.Database.SqlQueryRaw<Fila>(SqlExactas, new NpgsqlParameter("clave", clave)).ToListAsync(ct);
        return filas.Select(f => f.ADto()).ToList();
    }

    public async Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarAproximadasAsync(
        IReadOnlyList<string> textos, double umbral, int limite, CancellationToken ct = default)
    {
        if (textos.Count == 0) return [];
        // shortcut: similarity() sobre cada fila no usa el índice gin_trgm (el operador % ataría el umbral
        // a pg_trgm.similarity_threshold); alcanza con ~100 mil filas. Si pasa de 2 s (RNF-002), usar %.
        var filas = await _context.Database.SqlQueryRaw<Fila>(SqlAproximadas,
            new NpgsqlParameter("textos", textos.ToArray()),
            new NpgsqlParameter("umbral", umbral),
            new NpgsqlParameter("limite", limite)).ToListAsync(ct);
        return filas.Select(f => f.ADto()).ToList();
    }

    private sealed class Fila
    {
        public int Id { get; set; }
        public int ProveedorId { get; set; }
        public string Proveedor { get; set; } = null!;
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? NumeroOriginal { get; set; }
        public string? Marca { get; set; }
        public int? ArticuloId { get; set; }
        public string? ArticuloCodigoInterno { get; set; }
        public string? ArticuloNombre { get; set; }
        public double? Parecido { get; set; }

        public CandidatoCatalogoDto ADto() => new(Id, ProveedorId, Proveedor, Codigo, Nombre, NumeroOriginal, Marca, Parecido,
            ArticuloId is int id ? new ArticuloVinculadoDto(id, ArticuloCodigoInterno!, ArticuloNombre!) : null);
    }
}

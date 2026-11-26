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

    public Task<IReadOnlyList<string>?> TextosDeArticuloAsync(int articuloId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>?>(null);   // esqueleto: rojo

    public Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarExactasAsync(string clave, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CandidatoCatalogoDto>>([]);

    public Task<IReadOnlyList<CandidatoCatalogoDto>> BuscarAproximadasAsync(
        IReadOnlyList<string> textos, double umbral, int limite, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CandidatoCatalogoDto>>([]);
}

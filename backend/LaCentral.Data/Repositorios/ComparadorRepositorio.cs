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

    public Task<DatosComparador?> ObtenerAsync(int articuloId, CancellationToken ct = default)
        => Task.FromResult<DatosComparador?>(null);   // esqueleto: rojo
}

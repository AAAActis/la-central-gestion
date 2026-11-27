using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Comparador;

/// <summary>030 · HU-CMP-01 y 032 · HU-CMP-03: lo que cuesta un artículo en cada proveedor.</summary>
public class ObtenerComparadorUseCase
{
    public const string SinProveedores = "El artículo no tiene filas de catálogo vinculadas de ningún proveedor activo.";

    // 054 CA4: el orden del «máximo» entre estados de stock.
    private static int Rango(string? estado) => estado switch { "HAY" => 3, "A_CONFIRMAR" => 2, "NO_HAY" => 1, _ => 0 };

    private readonly IComparadorRepositorio _comparador;
    public ObtenerComparadorUseCase(IComparadorRepositorio comparador) => _comparador = comparador;

    public async Task<Result<ComparadorDto>> EjecutarAsync(int articuloId, CancellationToken ct = default)
    {
        var datos = await _comparador.ObtenerAsync(articuloId, ct);
        if (datos is null)
            return Result<ComparadorDto>.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");

        var masBarato = datos.Filas.Min(f => f.PrecioNeto);
        // 030 CA-002: del más barato al más caro; sin neto no se compara y va al final.
        var filas = datos.Filas
            .OrderBy(f => f.PrecioNeto is null).ThenBy(f => f.PrecioNeto).ThenBy(f => f.Proveedor).ThenBy(f => f.Codigo)
            .Select(f => new FilaComparadorDto(f.ProveedorId, f.Proveedor, f.UrlReferencia, f.Codigo, f.Nombre, f.Marca,
                f.PrecioNeto, f.PrecioLista, f.PrecioNeto - masBarato, f.EstadoStock, f.Stock, f.UrlImagen, f.UltimaLectura,
                f.ProveedorId == datos.UltimoProveedorId, f.UltimaCorridaFallida))
            .ToList();
        var existencias = datos.Filas.GroupBy(f => f.ProveedorId)
            .Select(g => new ExistenciaProveedorDto(g.Key,
                g.MaxBy(f => Rango(f.EstadoStock))!.EstadoStock, g.Max(f => f.Stock)))
            .OrderBy(e => e.ProveedorId).ToList();
        return Result<ComparadorDto>.Success(new(articuloId, filas, existencias, filas.Count == 0 ? SinProveedores : null));
    }
}

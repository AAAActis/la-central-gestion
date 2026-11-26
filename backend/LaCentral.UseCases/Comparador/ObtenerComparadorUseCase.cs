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

    public Task<Result<ComparadorDto>> EjecutarAsync(int articuloId, CancellationToken ct = default)
        => Task.FromResult(Result<ComparadorDto>.Success(new(articuloId, [], [], null)));   // esqueleto: rojo
}

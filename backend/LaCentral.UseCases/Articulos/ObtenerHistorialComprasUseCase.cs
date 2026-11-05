using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>016 · HU-ART-06: el historial de compras de un artículo.</summary>
public class ObtenerHistorialComprasUseCase
{
    private readonly IHistorialComprasRepositorio _historial;

    public ObtenerHistorialComprasUseCase(IHistorialComprasRepositorio historial) => _historial = historial;

    public async Task<Result<IReadOnlyList<RegistroHistorialCompraDto>>> EjecutarAsync(int articuloId, CancellationToken ct = default)
    {
        var registros = await _historial.HistorialComprasAsync(articuloId, ct);
        return registros is null
            ? Result<IReadOnlyList<RegistroHistorialCompraDto>>.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.")
            : Result<IReadOnlyList<RegistroHistorialCompraDto>>.Success(registros);
    }
}

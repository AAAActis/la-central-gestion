using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-02 · Ficha de artículo.</summary>
public class ObtenerArticuloDetalleUseCase
{
    private readonly IConsultaArticulosRepositorio _consulta;

    public ObtenerArticuloDetalleUseCase(IConsultaArticulosRepositorio consulta)
    {
        _consulta = consulta;
    }

    public Task<Result<FichaArticuloDto>> EjecutarAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Result<FichaArticuloDto>.Failure(TipoError.Invalido, "Sin implementar."));
}

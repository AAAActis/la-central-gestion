using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-02 · Búsqueda de artículos.</summary>
public class ConsultarArticulosUseCase
{
    private readonly IConsultaArticulosRepositorio _consulta;

    public ConsultarArticulosUseCase(IConsultaArticulosRepositorio consulta)
    {
        _consulta = consulta;
    }

    public async Task<Result<PaginaArticulosDto>> EjecutarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
        => Result<PaginaArticulosDto>.Success(await _consulta.BuscarAsync(consulta, ct));
}

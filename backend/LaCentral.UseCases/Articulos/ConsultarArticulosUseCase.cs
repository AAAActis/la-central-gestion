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

    /// <summary>
    /// Texto vacío o paginación inválida: Invalido, sin tocar la base. Sin resultados NO es
    /// un error: es una página vacía.
    /// </summary>
    public async Task<Result<PaginaArticulosDto>> EjecutarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
    {
        var validacion = consulta.Validar();
        if (!validacion.IsSuccess)
            return Result<PaginaArticulosDto>.Failure(validacion.Tipo, validacion.Error);

        return Result<PaginaArticulosDto>.Success(await _consulta.BuscarAsync(consulta, ct));
    }
}

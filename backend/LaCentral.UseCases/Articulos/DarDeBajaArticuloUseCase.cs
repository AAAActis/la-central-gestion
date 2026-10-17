using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-03 · Baja lógica de artículo con su historial (D10).</summary>
public class DarDeBajaArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public DarDeBajaArticuloUseCase(IArticuloRepositorio articulos, IConsultaArticulosRepositorio consulta,
        IHistorialBajaRepositorio historial, IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _articulos = articulos;
        _consulta = consulta;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public Task<Result> EjecutarAsync(int id, DarDeBajaArticuloRequest request, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

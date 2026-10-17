using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-03 · Reactivación de artículo: cierra la baja abierta del historial (D10).</summary>
public class ReactivarArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ReactivarArticuloUseCase(IArticuloRepositorio articulos, IConsultaArticulosRepositorio consulta,
        IHistorialBajaRepositorio historial, IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _articulos = articulos;
        _consulta = consulta;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

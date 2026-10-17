using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
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

    public async Task<Result> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (articulo.Activo)
            return Result.Failure(TipoError.Invalido, "El artículo ya está activo.");

        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // La fila conserva motivo y fecha de la última baja (D10); el historial se cierra.
        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (!await _articulos.ReactivarAsync(id, c))
                return Result.Failure(TipoError.Invalido, "El artículo ya está activo.");
            await _historial.CerrarAsync(EntidadConHistorialBaja.Articulo, id, DateTime.UtcNow, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}

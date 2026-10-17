using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
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

    public async Task<Result> EjecutarAsync(int id, DarDeBajaArticuloRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure(TipoError.Invalido, "Indicá el motivo de la baja del artículo.");

        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (!articulo.Activo)
            return Result.Failure(TipoError.Invalido, "El artículo ya está dado de baja.");

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        var motivo = request.Motivo.Trim();
        var fecha = DateTime.UtcNow;

        // D10: la fila y su historial, juntos o ninguno.
        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (!await _articulos.DarDeBajaAsync(id, motivo, fecha, c))
                return Result.Failure(TipoError.Invalido, "El artículo ya está dado de baja.");
            await _historial.AbrirAsync(EntidadConHistorialBaja.Articulo, id, motivo, fecha, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}

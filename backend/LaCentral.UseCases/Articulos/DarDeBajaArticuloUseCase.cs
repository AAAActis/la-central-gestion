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

        // 014 · CA-004: como el formulario (HU-UI-06), sin espacios en los bordes ni distinguir mayúsculas.
        if (!string.Equals(request.Confirmacion?.Trim(), articulo.CodigoInterno, StringComparison.OrdinalIgnoreCase))
            return Result.Failure(TipoError.Invalido, "El código interno reescrito no coincide con el del artículo.");

        // 014 · CA-002: lo que queda, con su cantidad y su sucursal.
        if (ConExistencia(articulo) is { } quedan)
            return Result.Failure(TipoError.Conflicto, quedan);

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        var motivo = request.Motivo.Trim();
        var fecha = DateTime.UtcNow;

        // D10: la fila y su historial, juntos o ninguno.
        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            // El UPDATE exige activo y sin existencia: si no escribió, se explica con lo que hay ahora.
            if (!await _articulos.DarDeBajaAsync(id, motivo, fecha, c))
                return await _consulta.ObtenerDetallePorIdAsync(id, c) is { } ahora && ConExistencia(ahora) is { } quedanAhora
                    ? Result.Failure(TipoError.Conflicto, quedanAhora)
                    : Result.Failure(TipoError.Invalido, "El artículo ya está dado de baja.");
            await _historial.AbrirAsync(EntidadConHistorialBaja.Articulo, id, motivo, fecha, usuarioId, c);
            return Result.Success();
        }, ct);
    }

    private static readonly System.Globalization.CultureInfo EsAr = System.Globalization.CultureInfo.GetCultureInfo("es-AR");

    /// <summary>Null si no queda nada. Una existencia negativa es un error de stock, no algo que quede.</summary>
    private static string? ConExistencia(ArticuloDetalleConsultaDto articulo)
    {
        var quedan = articulo.Existencias.Where(e => e.Cantidad > 0).Select(e => $"{e.Sucursal}: {e.Cantidad.ToString("0.###", EsAr)}").ToList();
        return quedan.Count == 0 ? null : $"No se puede dar de baja: el artículo todavía tiene existencia ({string.Join("; ", quedan)}).";
    }
}

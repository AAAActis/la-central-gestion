using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaCentral.Api.Middleware;

/// <summary>
/// Última red para lo que ningún caso de uso tradujo a Result. Las reglas de
/// negocio NO pasan por acá: viajan como Result y las traduce MapeoResultado.
///
/// Solo dos salidas posibles:
///   · violación de unicidad de una restricción reconocida → 409. Es la carrera
///     que el núcleo no puede evitar (dos altas simultáneas pasan la validación
///     previa y el índice rechaza la segunda).
///   · cualquier otra cosa → 500 genérico.
/// Ninguna respuesta lleva el mensaje, el tipo ni el stack de la excepción.
/// </summary>
public sealed class ManejadorExcepcionesGlobal : IExceptionHandler
{
    /// <summary>
    /// Restricciones únicas del esquema real con su mensaje para el usuario.
    /// Una 23505 sobre una restricción que no está acá es un error no previsto
    /// y se informa como 500: no se adivina qué significa.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> RestriccionesUnicasReconocidas =
        new Dictionary<string, string>
        {
            ["cliente_codigo_key"] = "Ya existe un cliente registrado con ese código.",
            ["cliente_cuit_cuil_key"] = "Ya existe un cliente registrado con ese CUIT/CUIL.",
            ["proveedor_codigo_key"] = "Ya existe un proveedor registrado con ese código.",
            ["proveedor_cuit_key"] = "Ya existe un proveedor registrado con ese CUIT.",
            ["usuario_nombre_usuario_key"] = "Ya existe un usuario con ese nombre.",
            ["articulo_codigo_interno_key"] = "Ya existe un artículo con ese código interno.",
            ["codigo_unico_global"] = "Ese código ya está asignado a otro artículo.",
            ["un_codigo_por_proveedor"] = "El artículo ya tiene un código vigente de ese proveedor.",
            ["ux_factura_compra_numero_activa"] = "Ya existe una factura de compra activa con ese número para el proveedor.",
            ["ux_cliente_hist_abierta"] = "El cliente ya tiene una baja abierta.",
            ["ux_proveedor_hist_abierta"] = "El proveedor ya tiene una baja abierta.",
            ["ux_articulo_hist_abierta"] = "El artículo ya tiene una baja abierta.",
            ["ux_usuario_hist_abierta"] = "El usuario ya tiene una baja abierta."
        };

    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ManejadorExcepcionesGlobal> _logger;

    public ManejadorExcepcionesGlobal(IProblemDetailsService problemDetails, ILogger<ManejadorExcepcionesGlobal> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken ct)
    {
        ProblemDetails problema;
        if (MensajeDeConflicto(excepcion) is { } mensaje)
        {
            _logger.LogWarning(excepcion, "Violación de unicidad traducida a 409.");
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto con los datos existentes.",
                Detail = mensaje
            };
        }
        else
        {
            _logger.LogError(excepcion, "Excepción no manejada.");
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno del servidor.",
                Detail = "Ocurrió un error inesperado. Si persiste, informá al administrador."
            };
        }

        contexto.Response.StatusCode = problema.Status.Value;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excepcion
        });
    }

    /// <summary>El mensaje para el usuario si es una violación de unicidad reconocida; si no, null.</summary>
    public static string? MensajeDeConflicto(Exception excepcion)
    {
        if (excepcion is not DbUpdateException { InnerException: PostgresException pg } ||
            pg.SqlState != PostgresErrorCodes.UniqueViolation ||
            pg.ConstraintName is null)
            return null;

        return RestriccionesUnicasReconocidas.TryGetValue(pg.ConstraintName, out var mensaje) ? mensaje : null;
    }
}

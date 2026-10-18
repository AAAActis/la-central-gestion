using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaCentral.Api.Middleware;

/// <summary>
/// Última red para lo que ningún caso de uso tradujo a Result. Las reglas de
/// negocio NO pasan por acá: viajan como Result y las traduce MapeoResultado.
///
/// Salidas posibles:
///   · violación de unicidad de una restricción reconocida → 409. Es la carrera
///     que el núcleo no puede evitar (dos altas simultáneas pasan la validación
///     previa y el índice rechaza la segunda).
///   · violación de un CHECK reconocido → 409. Es un dato existente que choca con
///     la operación (una baja con fecha futura que no admite la reactivación).
///   · texto que PostgreSQL no puede guardar (22021, p. ej. el carácter NUL) → 400,
///     en cualquier endpoint.
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
            ["ux_factura_venta_numero_activa"] = "Ya existe una factura de venta activa con ese número.",
            ["cliente_telefono_cliente_id_numero_key"] = "El cliente ya tiene registrado ese teléfono.",
            ["proveedor_telefono_proveedor_id_numero_key"] = "El proveedor ya tiene registrado ese teléfono.",
            ["ux_cliente_hist_abierta"] = "El cliente ya tiene una baja abierta.",
            ["ux_proveedor_hist_abierta"] = "El proveedor ya tiene una baja abierta.",
            ["ux_articulo_hist_abierta"] = "El artículo ya tiene una baja abierta.",
            ["ux_usuario_hist_abierta"] = "El usuario ya tiene una baja abierta."
        };

    /// <summary>
    /// CHECK del esquema real (23514) con su mensaje. Igual que con la unicidad, un
    /// CHECK que no está acá se informa como 500.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> RestriccionesCheckReconocidas =
        new Dictionary<string, string>
        {
            ["cliente_hist_fechas_coherentes"] = MensajeReactivacionAnterior,
            ["proveedor_hist_fechas_coherentes"] = MensajeReactivacionAnterior,
            ["articulo_hist_fechas_coherentes"] = MensajeReactivacionAnterior,
            ["usuario_hist_fechas_coherentes"] = MensajeReactivacionAnterior
        };

    private const string MensajeReactivacionAnterior =
        "La fecha de reactivación no puede ser anterior a la fecha de la baja.";

    public const string MensajeCaracteresNoValidos =
        "Un texto enviado tiene caracteres no válidos (por ejemplo, el carácter nulo).";

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
            _logger.LogWarning(excepcion, "Violación de restricción reconocida traducida a 409.");
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflicto con los datos existentes.",
                Detail = mensaje
            };
        }
        else if (EsTextoConCaracteresNoValidos(excepcion))
        {
            _logger.LogWarning(excepcion, "Texto con caracteres no válidos traducido a 400.");
            problema = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos inválidos.",
                Detail = MensajeCaracteresNoValidos
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

    /// <summary>El mensaje para el usuario si es una violación reconocida (unicidad o CHECK); si no, null.</summary>
    public static string? MensajeDeConflicto(Exception excepcion)
    {
        var pg = ExcepcionDePostgres(excepcion);
        if (pg?.ConstraintName is null)
            return null;

        var reconocidas = pg.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => RestriccionesUnicasReconocidas,
            PostgresErrorCodes.CheckViolation => RestriccionesCheckReconocidas,
            _ => null
        };
        return reconocidas is not null && reconocidas.TryGetValue(pg.ConstraintName, out var mensaje) ? mensaje : null;
    }

    /// <summary>True si PostgreSQL rechazó un texto por su codificación (22021).</summary>
    public static bool EsTextoConCaracteresNoValidos(Exception excepcion) =>
        ExcepcionDePostgres(excepcion)?.SqlState == PostgresErrorCodes.CharacterNotInRepertoire;

    // SaveChanges envuelve la PostgresException en DbUpdateException; ExecuteUpdate no.
    private static PostgresException? ExcepcionDePostgres(Exception excepcion) => excepcion switch
    {
        DbUpdateException { InnerException: PostgresException interna } => interna,
        PostgresException directa => directa,
        _ => null
    };
}

using LaCentral.UseCases.Comun;
using Microsoft.AspNetCore.Mvc;

namespace LaCentral.Api.Middleware;

/// <summary>
/// Traduce el Result que devuelven los casos de uso a una respuesta HTTP.
///
/// Existe para que los controladores no repitan la misma cadena de if, y
/// sobre todo para que el núcleo no conozca códigos HTTP: el caso de uso
/// dice QUÉ pasó, y esta clase decide CÓMO se comunica por HTTP.
/// </summary>
public static class MapeoResultado
{
    /// <summary>Para casos de uso que no devuelven valor: altas, bajas, reactivaciones.</summary>
    public static IActionResult AResultadoHttp(
        this ControllerBase controlador, Result resultado)
    {
        if (resultado.IsSuccess)
        {
            return controlador.NoContent();
        }

        return Falla(controlador, resultado.Tipo, resultado.Error);
    }

    /// <summary>Para casos de uso que devuelven un valor: login, consultas.</summary>
    public static IActionResult AResultadoHttp<T>(
        this ControllerBase controlador, Result<T> resultado)
    {
        if (resultado.IsSuccess)
        {
            return controlador.Ok(resultado.Value);
        }

        return Falla(controlador, resultado.Tipo, resultado.Error);
    }

    /// <summary>
    /// Punto único donde se elige el código HTTP de un fallo. Ningún controlador
    /// decide esto: si mañana cambia la traducción, se cambia acá y nada más.
    ///
    /// Problem() devuelve un cuerpo ProblemDetails (RFC 7807), que es el formato
    /// estándar de error de las APIs HTTP, no un string suelto.
    /// </summary>
    private static IActionResult Falla(
        ControllerBase controlador, TipoError tipo, string error)
    {
        var codigo = tipo switch
        {
            TipoError.NoEncontrado => StatusCodes.Status404NotFound,
            TipoError.Conflicto    => StatusCodes.Status409Conflict,
            TipoError.NoAutorizado => StatusCodes.Status401Unauthorized,
            TipoError.Invalido     => StatusCodes.Status400BadRequest,

            // Sin este caso por defecto, un TipoError que alguien agregue mañana
            // y no esté mapeado acá lanzaría SwitchExpressionException y el
            // usuario recibiría un 500. Degradar a 400 es preferible a reventar.
            _                      => StatusCodes.Status400BadRequest
        };

        return controlador.Problem(detail: error, statusCode: codigo);
    }
}

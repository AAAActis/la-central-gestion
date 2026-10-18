using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// Ejecuta varias escrituras como una sola operación atómica: confirma si la operación
/// devuelve éxito y descarta todo si devuelve un fallo o lanza una excepción.
/// </summary>
public interface IUnidadDeTrabajo
{
    Task<Result> EjecutarAsync(Func<CancellationToken, Task<Result>> operacion, CancellationToken ct = default);
}

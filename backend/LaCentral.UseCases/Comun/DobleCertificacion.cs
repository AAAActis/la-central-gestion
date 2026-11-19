using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Comun;

/// <summary>
/// Doble certificación de una anulación (B.4 del 07, 14/11/2026): la autoriza OTRO usuario,
/// ADMINISTRADOR y activo, que prueba quién es con su contraseña. La usan la 028 y la 057 (D-S6-23).
/// </summary>
public static class DobleCertificacion
{
    // Valores de la tabla rol: 1 = OPERADOR, 2 = ADMINISTRADOR (ver AuthenticarUsuarioCase).
    private const int RolAdministrador = 2;
    // Un solo mensaje para usuario inexistente, inactivo o clave mala: no revela quién existe.
    private const string AutorizacionInvalida = "La autorización no es válida: revisá el usuario y la contraseña de quien autoriza.";

    /// <summary>El Id de quien autoriza, o Invalido.</summary>
    public static async Task<Result<int>> VerificarAsync(IUsuarioRepositorio usuarios, IServicioHash hash,
        string? usuarioAutoriza, string? contrasenaAutoriza, int usuarioId, CancellationToken ct)
    {
        var autoriza = string.IsNullOrWhiteSpace(usuarioAutoriza)
            ? null
            : await usuarios.ObtenerPorNombreAsync(usuarioAutoriza.Trim(), ct);
        if (autoriza is null || !autoriza.Activo || contrasenaAutoriza is null
            || !hash.VerificarClave(contrasenaAutoriza, autoriza.HashContrasena))
            return Result<int>.Failure(TipoError.Invalido, AutorizacionInvalida);
        if (autoriza.Id == usuarioId)
            return Result<int>.Failure(TipoError.Invalido, "La anulación la tiene que autorizar otra persona, no quien la pide.");
        if (autoriza.RolId != RolAdministrador)
            return Result<int>.Failure(TipoError.Invalido, "Quien autoriza la anulación tiene que ser ADMINISTRADOR.");
        return Result<int>.Success(autoriza.Id);
    }
}

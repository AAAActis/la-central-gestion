using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Cuerpo del restablecimiento de contraseña. No pide la contraseña anterior:
/// el CA5 de HU-ACC-02 dice explícitamente que el Administrador la restablece
/// sin requerirla.
/// </summary>
public record RestablecerContrasenaRequest(
    [property: Required(ErrorMessage = "La contraseña nueva es obligatoria.")]
    [property: MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    string ClaveNueva);

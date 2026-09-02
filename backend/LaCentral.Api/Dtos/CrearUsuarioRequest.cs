using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada del alta de usuario: lo que el cliente manda por HTTP.
///
/// Es un tipo distinto del CrearUsuarioRequest de UseCases a propósito. Este
/// es el límite público de la API y valida formato; el del núcleo es lo que
/// el caso de uso necesita para trabajar. Si mañana cambia uno, el otro no
/// se entera.
/// </summary>
public record CrearUsuarioRequest(
    [property: Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [property: MaxLength(30, ErrorMessage = "El nombre de usuario no puede superar los 30 caracteres.")]
    string NombreUsuario,

    [property: Required(ErrorMessage = "La clave es obligatoria.")]
    [property: MinLength(6, ErrorMessage = "La clave debe tener al menos 6 caracteres.")]
    string Clave,

    [property: Required(ErrorMessage = "El rol es obligatorio.")]
    string Rol,

    [property: Required(ErrorMessage = "La sucursal es obligatoria.")]
    string Sucursal);

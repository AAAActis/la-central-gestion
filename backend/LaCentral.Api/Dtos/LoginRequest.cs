using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada del inicio de sesión.
///
/// A diferencia del alta, acá solo se valida que los campos vengan. No se
/// valida largo ni formato: hacerlo le diría a un atacante qué forma tienen
/// las credenciales válidas, y además el rechazo debe ser siempre el mismo
/// mensaje genérico (CA-002 de HU-ACC-01).
/// </summary>
public record LoginRequest(
    [property: Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    string NombreUsuario,

    [property: Required(ErrorMessage = "La clave es obligatoria.")]
    string Clave);

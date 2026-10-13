using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

public record LoginRequest
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    public string NombreUsuario { get; init; } = string.Empty;
    
    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Contrasena { get; init; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Cuerpo de la baja lógica genérica. El motivo es obligatorio.
/// </summary>
public record DarDeBajaRequest
{
    [Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string Motivo { get; init; } = string.Empty;
}
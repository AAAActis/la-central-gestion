using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>Cuerpo de la baja de artículo (HU-ART-03 y 014): motivo y el código interno reescrito.</summary>
public record BajaArticuloRequest
{
    [Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string Motivo { get; init; } = string.Empty;

    /// <summary>014 · CA-004: el código interno reescrito para confirmar. Lo compara el caso de uso.</summary>
    [MaxLength(40, ErrorMessage = "La confirmación no puede superar los 40 caracteres.")]
    public string? Confirmacion { get; init; }
}

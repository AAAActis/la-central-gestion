using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada para solicitar la baja lógica de un proveedor.
/// Requiere confirmación explícita (CUIT o Código) y un motivo que justifique la acción.
/// Mismo criterio que BajaClienteRequest.
/// </summary>
public record BajaProveedorRequest
{
    [Required(ErrorMessage = "La confirmación (Código o CUIT) es obligatoria.")]
    [MaxLength(20, ErrorMessage = "La confirmación no puede superar los 20 caracteres.")]
    public string CuitReescrito { get; init; } = string.Empty;

    [Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [MaxLength(255, ErrorMessage = "El motivo no puede superar los 255 caracteres.")]
    public string MotivoBaja { get; init; } = string.Empty;
}
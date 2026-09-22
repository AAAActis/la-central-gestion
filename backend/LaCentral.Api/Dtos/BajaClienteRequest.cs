using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada para solicitar la baja lógica de un cliente.
/// Requiere confirmación explícita (CUIT o Código) y un motivo que justifique la acción.
/// </summary>
public record BajaClienteRequest(
    [property: Required(ErrorMessage = "La confirmación (Código o CUIT) es obligatoria.")]
    [property: MaxLength(20, ErrorMessage = "La confirmación no puede superar los 20 caracteres.")]
    string Confirmacion,

    [property: Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [property: MaxLength(255, ErrorMessage = "El motivo no puede superar los 255 caracteres.")]
    string Motivo
);
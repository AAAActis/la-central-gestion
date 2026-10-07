using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

public record CrearClienteRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    public string Codigo { get; init; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [MaxLength(120, ErrorMessage = "La razón social no puede superar los 120 caracteres.")]
    public string RazonSocial { get; init; } = string.Empty;

    [MaxLength(13, ErrorMessage = "El CUIT no puede superar los 13 caracteres.")]
    public string? Cuit { get; init; }

    [Required(ErrorMessage = "La condición fiscal es obligatoria.")]
    [MaxLength(30, ErrorMessage = "La condición fiscal no puede superar los 30 caracteres.")]
    public string CondicionFiscal { get; init; } = string.Empty;

    [Required(ErrorMessage = "La condición de pago es obligatoria.")]
    [MaxLength(60, ErrorMessage = "La condición de pago no puede superar los 60 caracteres.")]
    public string CondicionPago { get; init; } = string.Empty;

    public List<string>? Telefonos { get; init; }

    public List<string>? Direcciones { get; init; }
}
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

public record CrearProveedorRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    public string Codigo { get; init; } = string.Empty;

    [Required(ErrorMessage = "La razón social es obligatoria.")]
    [MaxLength(120, ErrorMessage = "La razón social no puede superar los 120 caracteres.")]
    public string RazonSocial { get; init; } = string.Empty;

    [MaxLength(13, ErrorMessage = "El CUIT no puede superar los 13 caracteres.")]
    public string? Cuit { get; init; }

    [MaxLength(300, ErrorMessage = "La URL de referencia no puede superar los 300 caracteres.")]
    public string? UrlReferencia { get; init; }

    public List<string>? Telefonos { get; init; }

    public List<string>? Direcciones { get; init; }
}
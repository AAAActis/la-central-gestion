using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// HU-ART-03. CodigoInterno null = no se cambia (cambiarlo es solo de ADMINISTRADOR).
/// Los códigos que se omiten NO se retiran. Las reglas de negocio las valida el caso de uso.
/// </summary>
public record ModificarArticuloRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [MaxLength(60, ErrorMessage = "La ubicación en depósito no puede superar los 60 caracteres.")]
    public string? UbicacionDeposito { get; init; }

    [MaxLength(40, ErrorMessage = "El código interno no puede superar los 40 caracteres.")]
    public string? CodigoInterno { get; init; }

    public List<CodigoProveedorRequest>? CodigosProveedor { get; init; }
}

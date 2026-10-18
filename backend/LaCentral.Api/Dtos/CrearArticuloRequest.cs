using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// HU-ART-01. Sin código interno (lo genera el sistema), sin costo y sin precios
/// (nacen en null). Las reglas de negocio las valida el caso de uso.
/// </summary>
public record CrearArticuloRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El nombre no puede superar los 200 caracteres.")]
    public string Nombre { get; init; } = string.Empty;

    [MaxLength(60, ErrorMessage = "La ubicación en depósito no puede superar los 60 caracteres.")]
    public string? UbicacionDeposito { get; init; }

    public List<CodigoProveedorRequest>? CodigosProveedor { get; init; }
}

public record CodigoProveedorRequest
{
    public int ProveedorId { get; init; }

    [Required(ErrorMessage = "El código de proveedor es obligatorio.")]
    [MaxLength(60, ErrorMessage = "El código de proveedor no puede superar los 60 caracteres.")]
    public string Codigo { get; init; } = string.Empty;
}

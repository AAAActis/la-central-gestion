using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>N-9 · ubicación en depósito de un artículo en una sucursal. Vacío la borra.</summary>
public record AsignarUbicacionRequest
{
    [MaxLength(60, ErrorMessage = "La ubicación en depósito no puede superar los 60 caracteres.")]
    public string? UbicacionDeposito { get; init; }
}

using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>054a CA5 · El nombre del artículo que se da de alta desde la fila de catálogo.</summary>
public record AltaVinculadaRequest
{
    [Required(ErrorMessage = "El nombre del artículo es obligatorio.")]
    public string Nombre { get; init; } = string.Empty;
}

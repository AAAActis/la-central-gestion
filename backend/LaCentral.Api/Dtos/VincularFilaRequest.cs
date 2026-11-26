using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>054a · El artículo propio al que corresponde la fila de catálogo. El usuario sale de la sesión.</summary>
public record VincularFilaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el artículo al que corresponde la fila.")]
    public int ArticuloId { get; init; }
}

using System.ComponentModel.DataAnnotations;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;

namespace LaCentral.Api.Dtos;

/// <summary>
/// HU-FCO-07 (057). Sin usuario (sale de la sesión) ni tipo (es siempre A). La fecha es anulable
/// solo para que su falta sea un 400 y no el 01/01/0001. Las reglas las valida el caso de uso.
/// </summary>
public record RegistrarNotaCreditoCompraRequest
{
    public int ProveedorId { get; init; }
    [Required(ErrorMessage = "El número de la nota de crédito es obligatorio.")]
    public string Numero { get; init; } = string.Empty;
    [Required(ErrorMessage = "La fecha de emisión es obligatoria.")]
    public DateOnly? FechaEmision { get; init; }
    public string? Clase { get; init; }
    public decimal ImporteNeto { get; init; }
    public decimal? PercepcionIva { get; init; }
    public decimal? PercepcionIibb { get; init; }
    public decimal? PercepcionMunicipal { get; init; }
    public decimal? PercepcionGanancias { get; init; }
    public List<int>? FacturaIds { get; init; }
    public List<LineaNotaCreditoCompraRequest>? Lineas { get; init; }
    /// <summary>CA-010: la nota se cargó desde el PDF del proveedor.</summary>
    public bool DesdePdf { get; init; }

    public UseCases.NotasCreditoCompra.Dtos.RegistrarNotaCreditoCompraRequest ANucleo() => new(
        ProveedorId, Numero, FechaEmision!.Value, Clase, ImporteNeto, PercepcionIva, PercepcionIibb,
        PercepcionMunicipal, PercepcionGanancias, FacturaIds, Lineas, DesdePdf);
}

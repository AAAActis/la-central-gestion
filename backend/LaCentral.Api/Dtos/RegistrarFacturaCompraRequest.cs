using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// HU-FCO-01. Sin usuario (sale de la sesión) ni estado (nace ACTIVA). Las percepciones
/// (D8) son datos del comprobante: si no vienen, son cero. Las reglas de negocio las
/// valida el caso de uso.
/// </summary>
public record RegistrarFacturaCompraRequest
{
    public int ProveedorId { get; init; }

    [Required(ErrorMessage = "El número de factura es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El número de factura no puede superar los 30 caracteres.")]
    public string Numero { get; init; } = string.Empty;

    [Required(ErrorMessage = "La fecha de emisión es obligatoria.")]
    public DateOnly? FechaEmision { get; init; }

    public DateOnly? FechaVencimiento { get; init; }

    public decimal PercepcionIva { get; init; }
    public decimal PercepcionIibb { get; init; }
    public decimal PercepcionMunicipal { get; init; }
    public decimal PercepcionGanancias { get; init; }

    public List<LineaFacturaCompraRequest>? Lineas { get; init; }
}

/// <summary>Una línea. D9: el destino de stock es de la línea.</summary>
public record LineaFacturaCompraRequest
{
    public int ArticuloId { get; init; }
    public short SucursalId { get; init; }
    public decimal Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal DescuentoPorcentaje { get; init; }
}

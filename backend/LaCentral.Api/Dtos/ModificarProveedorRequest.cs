using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Modificación de proveedor (HU-PRO-03). #168: el PUT reemplaza todo, así que el tipo de
/// comprobante habitual tiene que venir en el cuerpo; null lo deja vacío.
/// </summary>
public record ModificarProveedorRequest
{
    private readonly string? _tipoComprobanteHabitual;

    public string RazonSocial { get; init; } = string.Empty;

    public string Cuit { get; init; } = string.Empty;

    public string? UrlReferencia { get; init; }

    public List<string>? Telefonos { get; init; }

    public List<string>? Direcciones { get; init; }

    /// <summary>"A", "B" o null. El deserializador solo lo asigna si el cuerpo trae la clave.</summary>
    public string? TipoComprobanteHabitual
    {
        get => _tipoComprobanteHabitual;
        init { _tipoComprobanteHabitual = value; TraeTipoComprobanteHabitual = true; }
    }

    [JsonIgnore]
    public bool TraeTipoComprobanteHabitual { get; private init; }
}

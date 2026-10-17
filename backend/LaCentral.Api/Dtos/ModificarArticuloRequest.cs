using System.Collections.Generic;

namespace LaCentral.Api.Dtos;

/// <summary>
/// HU-ART-03. CodigoInterno null = no se cambia (cambiarlo es solo de ADMINISTRADOR).
/// Los códigos que se omiten NO se retiran. Las reglas de negocio las valida el caso de uso.
/// </summary>
public record ModificarArticuloRequest
{
    public string Nombre { get; init; } = string.Empty;

    public string? UbicacionDeposito { get; init; }

    public string? CodigoInterno { get; init; }

    public List<CodigoProveedorRequest>? CodigosProveedor { get; init; }
}

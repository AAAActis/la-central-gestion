namespace LaCentral.UseCases.Articulos.Dtos;

/// <summary>
/// HU-ART-03. CodigoInterno null = no se cambia. CodigosProveedor son los que se asocian:
/// un código que el artículo ya tiene no es conflicto, y uno que se omite NO se retira
/// (retirar es una operación explícita, solo de administrador).
/// </summary>
public sealed record ModificarArticuloRequest(
    string Nombre, string? CodigoInterno,
    IReadOnlyList<CodigoProveedorRequest>? CodigosProveedor);

/// <summary>014 · CA-004: Confirmacion es el código interno reescrito por el usuario.</summary>
public sealed record DarDeBajaArticuloRequest(string Motivo, string Confirmacion);

namespace LaCentral.UseCases.Articulos.Dtos;

/// <summary>
/// HU-ART-03. CodigoInterno null = no se cambia. CodigosProveedor son los que se asocian:
/// un código que el artículo ya tiene no es conflicto, y uno que se omite NO se retira
/// (retirar es una operación explícita, solo de administrador).
/// </summary>
public sealed record ModificarArticuloRequest(
    string Nombre, string? UbicacionDeposito, string? CodigoInterno,
    IReadOnlyList<CodigoProveedorRequest>? CodigosProveedor);

public sealed record DarDeBajaArticuloRequest(string Motivo);

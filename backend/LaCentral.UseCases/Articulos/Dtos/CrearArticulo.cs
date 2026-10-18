namespace LaCentral.UseCases.Articulos.Dtos;

/// <summary>Un código de proveedor para el artículo. D2: uno por proveedor.</summary>
public sealed record CodigoProveedorRequest(int ProveedorId, string Codigo);

/// <summary>
/// HU-ART-01. El código interno NO viene en la petición: lo genera el sistema.
/// Costo y precios tampoco: nacen en null hasta la primera factura de compra.
/// </summary>
public sealed record CrearArticuloRequest(
    string Nombre, string? UbicacionDeposito, IReadOnlyList<CodigoProveedorRequest> CodigosProveedor);

public sealed record CrearArticuloResponse(int Id, string CodigoInterno);

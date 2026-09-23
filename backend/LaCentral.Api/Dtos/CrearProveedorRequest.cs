using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada del alta de proveedor, con sus datos multivaluados.
///
/// Los largos salen de la tabla `proveedor`: codigo varchar(20),
/// razon_social varchar(120), cuit varchar(13), url_referencia varchar(300).
/// Igual que en CrearClienteRequest, validarlos acá da un 400 claro en vez
/// de reventar el SaveChanges con un 500.
/// </summary>
public record CrearProveedorRequest(
    [property: Required(ErrorMessage = "El código es obligatorio.")]
    [property: MaxLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    string Codigo,

    [property: Required(ErrorMessage = "La razón social es obligatoria.")]
    [property: MaxLength(120, ErrorMessage = "La razón social no puede superar los 120 caracteres.")]
    string RazonSocial,

    // Opcional: no todos los proveedores tienen CUIT cargado.
    [property: MaxLength(13, ErrorMessage = "El CUIT no puede superar los 13 caracteres.")]
    string? Cuit,

    [property: MaxLength(300, ErrorMessage = "La URL de referencia no puede superar los 300 caracteres.")]
    string? UrlReferencia,

    List<string>? Telefonos,

    List<string>? Direcciones);

using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Contrato de entrada del alta de cliente, con sus datos multivaluados.
///
/// Los largos salen de la tabla `cliente`: codigo varchar(20),
/// razon_social varchar(120), cuit_cuil varchar(13). Validarlos acá hace que
/// un dato demasiado largo vuelva como 400 con un mensaje claro, en vez de
/// llegar a la base y reventar el SaveChanges con un 500.
/// </summary>
public record CrearClienteRequest(
    [property: Required(ErrorMessage = "El código es obligatorio.")]
    [property: MaxLength(20, ErrorMessage = "El código no puede superar los 20 caracteres.")]
    string Codigo,

    [property: Required(ErrorMessage = "La razón social es obligatoria.")]
    [property: MaxLength(120, ErrorMessage = "La razón social no puede superar los 120 caracteres.")]
    string RazonSocial,

    // Opcional a propósito: 1580 de los 2108 clientes importados no tienen
    // CUIT cargado, así que la clave dura del negocio es el código heredado
    // de Multisoft y no el CUIT.
    [property: MaxLength(13, ErrorMessage = "El CUIT no puede superar los 13 caracteres.")]
    string? Cuit,

    // Listas anidadas: viajan como arreglos JSON y se guardan en
    // cliente_telefono y cliente_direccion, en la misma operación que el
    // cliente. Son opcionales: un cliente puede no tener ninguno.
    List<string>? Telefonos,

    List<string>? Direcciones);

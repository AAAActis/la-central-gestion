using System.ComponentModel.DataAnnotations;

namespace LaCentral.Api.Dtos;

/// <summary>
/// Cuerpo de la baja lógica. El motivo es obligatorio: la base lo exige por
/// restricción y la HU lo pide como criterio.
/// </summary>
public record DarDeBajaRequest(
    [property: Required(ErrorMessage = "El motivo de la baja es obligatorio.")]
    [property: MaxLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    string Motivo);

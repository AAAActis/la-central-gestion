namespace LaCentral.UseCases.Comun;

/// <summary>
/// Clasifica POR QUÉ falló un caso de uso, sin nombrar ningún código HTTP.
///
/// El núcleo no conoce HTTP: dice "esto fue un conflicto" y la capa API
/// decide que un conflicto se comunica con un 409. Si mañana el sistema se
/// expusiera por otro medio, el núcleo no cambia.
/// </summary>
public enum TipoError
{
    /// <summary>Sin error. Es el valor de un Result exitoso.</summary>
    Ninguno,

    /// <summary>El recurso pedido no existe.</summary>
    NoEncontrado,

    /// <summary>Choca con el estado actual de los datos: nombre duplicado, baja del último administrador.</summary>
    Conflicto,

    /// <summary>Los datos recibidos no cumplen una regla de negocio.</summary>
    Invalido,

    /// <summary>Quien opera no tiene permiso, o las credenciales no son válidas.</summary>
    NoAutorizado
}

namespace LaCentral.UseCases.Puertos;

public interface IContextoUsuario
{
    int UsuarioId { get; }
    string NombreUsuario { get; }
    short SucursalId { get; }

    /// <summary>
    /// El rol de la sesión autenticada, nunca del cuerpo de la petición. Cambiar el código
    /// interno de un artículo o retirar un código de proveedor es solo de ADMINISTRADOR.
    /// </summary>
    bool EsAdministrador { get; }
}

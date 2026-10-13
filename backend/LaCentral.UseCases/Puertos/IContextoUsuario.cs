namespace LaCentral.UseCases.Puertos;

public interface IContextoUsuario
{
    int UsuarioId { get; }
    string NombreUsuario { get; }
    short SucursalId { get; }
}
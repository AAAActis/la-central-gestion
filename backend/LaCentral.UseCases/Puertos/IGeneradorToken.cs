using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IGeneradorToken
{
    string GenerarToken(int usuarioId, string nombreUsuario, string nombreRol, short sucursalId);
}
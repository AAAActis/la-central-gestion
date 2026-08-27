using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IGeneradorToken
{
    string GenerarToken(Usuario usuario, string nombreRol);
}
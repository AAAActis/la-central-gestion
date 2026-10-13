namespace LaCentral.UseCases.Puertos;

public interface IServicioHash
{
    string HashearClave(string claveTextoPlano);
    bool VerificarClave(string claveTextoPlano, string hash);
}
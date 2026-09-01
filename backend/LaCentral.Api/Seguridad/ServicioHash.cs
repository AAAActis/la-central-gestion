using LaCentral.UseCases.Puertos;
using BCrypt.Net;

namespace LaCentral.Data.Servicios;

public class ServicioHash : IServicioHash
{
    public string HashearClave(string claveTextoPlano)
    {
        // Usa un work factor por defecto (costo computacional) seguro.
        return BCrypt.Net.BCrypt.HashPassword(claveTextoPlano);
    }

    public bool VerificarClave(string claveTextoPlano, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(claveTextoPlano, hash);
    }
}
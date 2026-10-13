namespace LaCentral.UseCases.Models;
public class AuthenticarUsuarioRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}
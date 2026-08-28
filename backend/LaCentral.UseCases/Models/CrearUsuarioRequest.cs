namespace LaCentral.UseCases.Models;

public class CrearUsuarioRequest
{
    public string NombreUsuario { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string Sucursal { get; set; } = string.Empty;
}
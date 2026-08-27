namespace LaCentral.UseCases.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string HashContrasena { get; set; } = null!;
    public string SucursalId { get; set; } = null!;
    public string RolId { get; set; } = null!;
    public bool Activo { get; set; }
}
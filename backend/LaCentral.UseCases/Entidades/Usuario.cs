namespace LaCentral.UseCases.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string HashContrasena { get; set; } = null!;
    public int SucursalId { get; set; }
    public int RolId { get; set; }
    public bool Activo { get; set; }
}
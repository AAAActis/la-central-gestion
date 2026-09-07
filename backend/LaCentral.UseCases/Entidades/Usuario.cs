namespace LaCentral.UseCases.Entidades;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = null!;
    public string HashContrasena { get; set; } = null!;
    public int SucursalId { get; set; }
    public int RolId { get; set; }
    public bool Activo { get; set; }

    // Baja lógica. La fila nunca se borra: el historial tiene que poder
    // seguir mostrando quién ejecutó cada operación (CA3 de HU-ACC-02).
    //
    // La base tiene una restricción `usuario_baja_coherente`: si el usuario
    // está activo los dos campos van nulos, y si no lo está los dos van
    // cargados. Cualquier código que toque Activo tiene que mover los tres
    // valores junto.
    public string? MotivoBaja { get; set; }
    public DateTime? FechaBaja { get; set; }
}

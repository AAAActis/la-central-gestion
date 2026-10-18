namespace LaCentral.UseCases.Entidades;

/// <summary>D10: entidades con historial de baja en una tabla 1:N propia.</summary>
public enum EntidadConHistorialBaja
{
    Cliente,
    Proveedor,
    Articulo,
    Usuario
}

/// <summary>
/// D10: una baja del historial. FechaReactivacion null = baja abierta. Al reactivar,
/// la fila conserva motivo y fecha: es el rastro que HU-CLI-05 CA-003 exige no perder.
/// </summary>
public class RegistroBaja
{
    public int Id { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaBaja { get; set; }
    public DateTime? FechaReactivacion { get; set; }
    public int? UsuarioBajaId { get; set; }
    public int? UsuarioReactivacionId { get; set; }
}

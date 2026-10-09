namespace LaCentral.Data.Models;

// El scaffold interpreta el índice único parcial como una relación 1:1.
// D10 permite muchas bajas cerradas y solo una abierta por entidad.
public partial class Cliente
{
    public ICollection<ClienteHistorialBaja> HistorialesBaja { get; set; } = new List<ClienteHistorialBaja>();
}

public partial class Proveedor
{
    public ICollection<ProveedorHistorialBaja> HistorialesBaja { get; set; } = new List<ProveedorHistorialBaja>();
}

public partial class Articulo
{
    public ICollection<ArticuloHistorialBaja> HistorialesBaja { get; set; } = new List<ArticuloHistorialBaja>();
}

public partial class Usuario
{
    public ICollection<UsuarioHistorialBaja> HistorialesBaja { get; set; } = new List<UsuarioHistorialBaja>();
}

using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.UseCases;
using LaCentral.UseCases.Proveedores;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// Casos de uso de baja y reactivación con los repositorios reales, todos sobre el
/// MISMO contexto (como el alcance scoped de la API). Solo la sesión es un doble.
/// </summary>
public static class CasosDeBajaPostgreSql
{
    public static DarDeBajaClienteUseCase DarDeBajaCliente(LaCentralDbContext contexto, int usuarioId)
        => new(new ClienteRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    public static ReactivarClienteUseCase ReactivarCliente(LaCentralDbContext contexto, int usuarioId)
        => new(new ClienteRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    public static DarDeBajaProveedorUseCase DarDeBajaProveedor(LaCentralDbContext contexto, int usuarioId)
        => new(new ProveedorRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    public static ReactivarProveedorUseCase ReactivarProveedor(LaCentralDbContext contexto, int usuarioId)
        => new(new ProveedorRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    public static DarDeBajaUsuarioUseCase DarDeBajaUsuario(LaCentralDbContext contexto, int usuarioId)
        => new(new UsuarioRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    public static ReactivarUsuarioUseCase ReactivarUsuario(LaCentralDbContext contexto, int usuarioId)
        => new(new UsuarioRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);
}

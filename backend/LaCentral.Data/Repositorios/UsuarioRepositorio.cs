using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models; // La base de datos
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Repositorios;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly LaCentralDbContext _context;

    public UsuarioRepositorio(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task CrearAsync(LaCentral.UseCases.Entidades.Usuario usuario, CancellationToken cancellationToken = default)
    {
        // Mapeo inverso: Entidad de dominio pura -> Entidad de EF Core
        var usuarioBd = new LaCentral.Data.Models.Usuario
        {
            NombreUsuario = usuario.NombreUsuario,
            HashContrasena = usuario.HashContrasena,
            SucursalId = (short)usuario.SucursalId, // Conversión explícita a short
            RolId = (short)usuario.RolId,           // Conversión explícita a short
            Activo = usuario.Activo
        };

        await _context.Usuarios.AddAsync(usuarioBd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        // AnyAsync es más eficiente que traer todo el objeto solo para saber si existe
        return await _context.Usuarios
            .AnyAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken);
    }

    public Task<LaCentral.UseCases.Entidades.Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
    {
        // Reutilizamos la sobrecarga nueva pasándole un token vacío para no duplicar código
        return ObtenerPorNombreAsync(nombreUsuario, CancellationToken.None);
    }

    public async Task<LaCentral.UseCases.Entidades.Usuario?> ObtenerPorNombreAsync(string nombreUsuario, CancellationToken cancellationToken = default)
    {
        // 1. Consulta con EF Core usando la entidad scaffoldeada
        var usuarioBd = await _context.Usuarios
            .SingleOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, cancellationToken);

        if (usuarioBd == null) return null;

        // 2. Mapeo a la entidad pura que exige UseCases
        return new LaCentral.UseCases.Entidades.Usuario
        {
            Id = usuarioBd.Id,
            NombreUsuario = usuarioBd.NombreUsuario,
            HashContrasena = usuarioBd.HashContrasena,
            SucursalId = usuarioBd.SucursalId, 
            RolId = usuarioBd.RolId,           
            Activo = usuarioBd.Activo          
        };
    }
}
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

    public async Task<LaCentral.UseCases.Entidades.Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
    {
        // 1. Consulta con EF Core usando la entidad scaffoldeada
        var usuarioBd = await _context.Usuarios
            .SingleOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

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
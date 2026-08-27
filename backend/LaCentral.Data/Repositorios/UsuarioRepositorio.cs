using Microsoft.EntityFrameworkCore;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Models;
using LaCentral.Data;

namespace LaCentral.Data.Repositorios;

public class UsuarioRepositorio : IUsuarioRepositorio
{
    private readonly LaCentralDbContext _context;

    public UsuarioRepositorio(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> ObtenerPorNombreAsync(string nombreUsuario)
    {
        return await _context.Usuarios
            .SingleOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);
    }
}
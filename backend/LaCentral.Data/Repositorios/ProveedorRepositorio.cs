using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models; // Modelos generados por EF Core
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Repositorios;

public class ProveedorRepositorio : IProveedorRepositorio
{
    private readonly LaCentralDbContext _context;

    public ProveedorRepositorio(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(LaCentral.UseCases.Entidades.Proveedor proveedor, CancellationToken ct = default)
    {
        var proveedorBd = new LaCentral.Data.Models.Proveedor
        {
            RazonSocial = proveedor.RazonSocial,
            Cuit = proveedor.Cuit, // Cambiar a CuitCuil si la DB lo llama así
            UrlReferencia = proveedor.UrlReferencia,
            Activo = proveedor.Activo,
            
            // Mapeo 1:N
            ProveedorTelefonos = proveedor.Telefonos
                .Select(t => new ProveedorTelefono { Numero = t })
                .ToList(),
            
            ProveedorDireccions = proveedor.Direcciones
                .Select(d => new ProveedorDireccion { Calle = d })
                .ToList()
        };

        await _context.Proveedors.AddAsync(proveedorBd, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default)
    {
        return _context.Proveedors.AnyAsync(p => p.Cuit == cuit, ct); // Cambiar a CuitCuil si hace falta
    }

    public Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default)
    {
        return _context.Proveedors.AnyAsync(p => p.RazonSocial == razonSocial, ct);
    }
}
using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models; 
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
            Codigo = proveedor.Codigo,
            RazonSocial = proveedor.RazonSocial,
            Cuit = proveedor.Cuit, 
            UrlReferencia = proveedor.UrlReferencia,
            Activo = proveedor.Activo,
            
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
        return _context.Proveedors.AnyAsync(p => p.Cuit == cuit, ct); 
    }

    public Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default)
    {
        return _context.Proveedors.AnyAsync(p => p.RazonSocial == razonSocial, ct);
    }
}
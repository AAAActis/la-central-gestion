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
    public async Task<LaCentral.UseCases.Entidades.Proveedor?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
    {
        var bd = await _context.Proveedors // Usá el nombre exacto que corregimos (ej. Proveedors si quedó así)
            .Include(p => p.ProveedorTelefonos)
            .Include(p => p.ProveedorDireccions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (bd == null) return null;

        return new LaCentral.UseCases.Entidades.Proveedor
        {
            Id = bd.Id,
            RazonSocial = bd.RazonSocial,
            Cuit = bd.Cuit, // Asegurá que coincida con Cuit o CuitCuil según tu modelo
            UrlReferencia = bd.UrlReferencia,
            Activo = bd.Activo,
            Telefonos = bd.ProveedorTelefonos.Select(t => t.Numero ?? string.Empty).ToList(),
            Direcciones = bd.ProveedorDireccions.Select(d => d.Calle ?? string.Empty).ToList()
        };
    }

    public async Task<LaCentral.UseCases.Entidades.Proveedor?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default)
    {
        var bd = await _context.Proveedors.FirstOrDefaultAsync(p => p.Cuit == cuit, ct);
        if (bd == null) return null;

        return new LaCentral.UseCases.Entidades.Proveedor
        {
            Id = bd.Id,
            RazonSocial = bd.RazonSocial,
            Cuit = bd.Cuit ?? string.Empty
        };
    }

    public async Task ActualizarAsync(LaCentral.UseCases.Entidades.Proveedor proveedor, CancellationToken ct = default)
    {
        var bd = await _context.Proveedors
            .Include(p => p.ProveedorTelefonos)
            .Include(p => p.ProveedorDireccions)
            .SingleOrDefaultAsync(p => p.Id == proveedor.Id, ct);

        if (bd == null) return;

        bd.RazonSocial = proveedor.RazonSocial;
        bd.Cuit = proveedor.Cuit;
        bd.UrlReferencia = proveedor.UrlReferencia;

        var telsABorrar = bd.ProveedorTelefonos.Where(t => !proveedor.Telefonos.Contains(t.Numero ?? string.Empty)).ToList();
        foreach (var t in telsABorrar) _context.Remove(t);

        var telsNuevos = proveedor.Telefonos.Where(t => !bd.ProveedorTelefonos.Any(b => b.Numero == t)).ToList();
        foreach (var t in telsNuevos) bd.ProveedorTelefonos.Add(new LaCentral.Data.Models.ProveedorTelefono { Numero = t });

        var dirsABorrar = bd.ProveedorDireccions.Where(d => !proveedor.Direcciones.Contains(d.Calle ?? string.Empty)).ToList();
        foreach (var d in dirsABorrar) _context.Remove(d);

        var dirsNuevas = proveedor.Direcciones.Where(d => !bd.ProveedorDireccions.Any(b => b.Calle == d)).ToList();
        foreach (var d in dirsNuevas) bd.ProveedorDireccions.Add(new LaCentral.Data.Models.ProveedorDireccion { Calle = d });

        await _context.SaveChangesAsync(ct);
    }
}
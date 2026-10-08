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
            
            ProveedorTelefono = proveedor.Telefonos
                .Select(t => new ProveedorTelefono { Numero = t })
                .ToList(),
            
            ProveedorDireccion = proveedor.Direcciones
                .Select(d => new ProveedorDireccion { Calle = d })
                .ToList()
        };

        await _context.Proveedor.AddAsync(proveedorBd, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default)
    {
        return _context.Proveedor.AnyAsync(p => p.Cuit == cuit, ct); 
    }

    public Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken ct = default)
    {
        return _context.Proveedor.AnyAsync(p => p.RazonSocial == razonSocial, ct);
    }

    // --- MÉTODO FALTANTE QUE ROMPÍA LA INTERFAZ ---
    private const double UmbralSimilitudRazonSocial = 0.2;

    public async Task<IReadOnlyList<LaCentral.UseCases.Entidades.Proveedor>> BuscarAsync(
        string texto, bool incluirInactivos, CancellationToken ct = default)
    {
        var query = _context.Proveedor.AsQueryable();

        if (!incluirInactivos)
        {
            query = query.Where(p => p.Activo);
        }

        var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());

        if (soloDigitos.Length == 11)
        {
            query = query.Where(p => p.Cuit == texto);
        }
        else
        {
            query = query
                .Where(p => EF.Functions.TrigramsSimilarity(p.RazonSocial, texto) > UmbralSimilitudRazonSocial)
                .OrderByDescending(p => EF.Functions.TrigramsSimilarity(p.RazonSocial, texto));
        }

        var bd = await query.ToListAsync(ct);

        return bd.Select(p => new LaCentral.UseCases.Entidades.Proveedor
        {
            Id = p.Id,
            Codigo = p.Codigo,
            RazonSocial = p.RazonSocial,
            Cuit = p.Cuit,
            UrlReferencia = p.UrlReferencia,
            Activo = p.Activo
        }).ToList();
    }
    // ----------------------------------------------

    public async Task<LaCentral.UseCases.Entidades.Proveedor?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
    {
        var bd = await _context.Proveedor 
            .Include(p => p.ProveedorTelefono)
            .Include(p => p.ProveedorDireccion)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (bd == null) return null;

        return new LaCentral.UseCases.Entidades.Proveedor
        {
            Id = bd.Id,
            Codigo = bd.Codigo,
            RazonSocial = bd.RazonSocial,
            Cuit = bd.Cuit, 
            UrlReferencia = bd.UrlReferencia,
            Activo = bd.Activo,
            MotivoBaja = bd.MotivoBaja, // <--- FALTABA ESTA LÍNEA
            Telefonos = bd.ProveedorTelefono.Select(t => t.Numero ?? string.Empty).ToList(),
            Direcciones = bd.ProveedorDireccion.Select(d => d.Calle ?? string.Empty).ToList()
        };
    }

    public async Task<LaCentral.UseCases.Entidades.Proveedor?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default)
    {
        var bd = await _context.Proveedor.FirstOrDefaultAsync(p => p.Cuit == cuit, ct);
        if (bd == null) return null;

        return new LaCentral.UseCases.Entidades.Proveedor
        {
            Id = bd.Id,
            Codigo = bd.Codigo,
            RazonSocial = bd.RazonSocial,
            Cuit = bd.Cuit,
            Activo = bd.Activo
        };
    }

    public async Task ActualizarAsync(LaCentral.UseCases.Entidades.Proveedor proveedor, CancellationToken ct = default)
    {
        var bd = await _context.Proveedor
            .Include(p => p.ProveedorTelefono)
            .Include(p => p.ProveedorDireccion)
            .SingleOrDefaultAsync(p => p.Id == proveedor.Id, ct);

        if (bd == null) return;

        bd.RazonSocial = proveedor.RazonSocial;
        bd.Cuit = proveedor.Cuit;
        bd.UrlReferencia = proveedor.UrlReferencia;

        // FIX CRÍTICO 2: Mapeo de estado y motivo
        bool estadoAnterior = bd.Activo;
        bd.Activo = proveedor.Activo;
        bd.MotivoBaja = proveedor.MotivoBaja;

        // Si transiciona de Activo a Inactivo, estampa la fecha
        if (estadoAnterior && !proveedor.Activo)
        {
            bd.FechaBaja = DateTime.UtcNow;
        }

        var telsABorrar = bd.ProveedorTelefono.Where(t => !proveedor.Telefonos.Contains(t.Numero ?? string.Empty)).ToList();
        foreach (var t in telsABorrar) _context.Remove(t);

        var telsNuevos = proveedor.Telefonos.Where(t => !bd.ProveedorTelefono.Any(b => b.Numero == t)).ToList();
        foreach (var t in telsNuevos) bd.ProveedorTelefono.Add(new LaCentral.Data.Models.ProveedorTelefono { Numero = t });

        var dirsABorrar = bd.ProveedorDireccion.Where(d => !proveedor.Direcciones.Contains(d.Calle ?? string.Empty)).ToList();
        foreach (var d in dirsABorrar) _context.Remove(d);

        var dirsNuevas = proveedor.Direcciones.Where(d => !bd.ProveedorDireccion.Any(b => b.Calle == d)).ToList();
        foreach (var d in dirsNuevas) bd.ProveedorDireccion.Add(new LaCentral.Data.Models.ProveedorDireccion { Calle = d });

        await _context.SaveChangesAsync(ct);
    }   
}
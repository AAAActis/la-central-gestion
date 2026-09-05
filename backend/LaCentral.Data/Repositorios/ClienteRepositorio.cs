using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models; // La base de datos
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Repositorios;

public class ClienteRepositorio : IClienteRepositorio
{
    private readonly LaCentralDbContext _context;

    public ClienteRepositorio(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(LaCentral.UseCases.Entidades.Cliente cliente, CancellationToken cancellationToken = default)
    {
        // Mapeo inverso: Entidad de dominio pura -> Entidad de EF Core
        var clienteBd = new LaCentral.Data.Models.Cliente
        {
            Codigo = cliente.Codigo,
            RazonSocial = cliente.RazonSocial,
            CuitCuil = cliente.Cuit,
            Activo = true, // Por defecto al dar de alta
            CondicionFiscal = cliente.CondicionFiscal,
            CondicionPago = cliente.CondicionPago,

            // Transformamos la List<string> del dominio a los modelos de EF Core
            ClienteTelefonos = cliente.Telefonos
            .Where(tel => !string.IsNullOrWhiteSpace(tel))
            .Select(tel => new ClienteTelefono { Numero = tel })
            .ToList(),
    
            ClienteDireccions = cliente.Direcciones
            .Where(dir => !string.IsNullOrWhiteSpace(dir))
            .Select(dir => new ClienteDireccion { Calle = dir })
            .ToList()

            
        };

        await _context.Clientes.AddAsync(clienteBd, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .AnyAsync(c => c.Codigo == codigo, cancellationToken);
    }

    public Task<bool> ExisteCuitAsync(string cuit, CancellationToken ct = default)
    {
        return _context.Clientes
            .AnyAsync(c => c.CuitCuil == cuit, ct);
    }

    public async Task<bool> ExisteRazonSocialAsync(string razonSocial, CancellationToken cancellationToken = default)
    {
        return await _context.Clientes
            .AnyAsync(c => c.RazonSocial == razonSocial, cancellationToken);
    }

    
}
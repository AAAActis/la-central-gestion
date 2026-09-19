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

    // HU-CLI-02: por debajo de este umbral la similitud de trigramas empieza a
    // traer resultados que no tienen nada que ver con lo buscado.
    private const double UmbralSimilitudRazonSocial = 0.2;

    public async Task<IReadOnlyList<LaCentral.UseCases.Entidades.Cliente>> BuscarAsync(
        string texto, bool incluirInactivos, CancellationToken cancellationToken = default)
    {
        var query = _context.Clientes.AsQueryable();

        if (!incluirInactivos)
        {
            query = query.Where(c => c.Activo);
        }

        // Detecta CUIT/CUIL por cantidad de dígitos (11), sin importar guiones u
        // otro formato que haya tipeado el operador.
        var soloDigitos = new string(texto.Where(char.IsDigit).ToArray());

        if (soloDigitos.Length == 11)
        {
            // Coincidencia exacta: igual criterio que ExisteCuitAsync, sin normalizar
            // el valor guardado en la base.
            query = query.Where(c => c.CuitCuil == texto);
        }
        else
        {
            // Búsqueda difusa por Razón Social, usando el índice GIN
            // ix_cliente_razon_social_trgm ya existente en la base.
            query = query
                .Where(c => EF.Functions.TrigramsSimilarity(c.RazonSocial, texto) > UmbralSimilitudRazonSocial)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.RazonSocial, texto));
        }

        var clientesBd = await query.ToListAsync(cancellationToken);

        // Mapeo inverso: Entidad de EF Core -> Entidad de dominio pura.
        return clientesBd
            .Select(c => new LaCentral.UseCases.Entidades.Cliente
            {
                Codigo = c.Codigo,
                RazonSocial = c.RazonSocial,
                Cuit = c.CuitCuil,
                CondicionFiscal = c.CondicionFiscal ?? string.Empty,
                CondicionPago = c.CondicionPago ?? string.Empty,
                Activo = c.Activo
            })
            .ToList();
    }
}
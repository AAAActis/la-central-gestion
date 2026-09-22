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

    public async Task<LaCentral.UseCases.Entidades.Cliente?> ObtenerDetallePorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Usamos los nombres reales de las propiedades de navegación de LaCentral.Data.Models.Cliente
        var clienteBd = await _context.Clientes
            .Include(c => c.ClienteTelefonos)
            .Include(c => c.ClienteDireccions)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (clienteBd == null) return null;

        return new LaCentral.UseCases.Entidades.Cliente
        {
            // Se elimina Id = clienteBd.Id porque el dominio exige usar Codigo
            Codigo = clienteBd.Codigo,
            RazonSocial = clienteBd.RazonSocial,
            Cuit = clienteBd.CuitCuil,
            CondicionFiscal = clienteBd.CondicionFiscal ?? string.Empty,
            CondicionPago = clienteBd.CondicionPago ?? string.Empty,
            Activo = clienteBd.Activo,
            
            // Mapeamos leyendo desde ClienteTelefonos y ClienteDireccions
            Telefonos = clienteBd.ClienteTelefonos.Select(t => t.Numero).ToList(), 
            Direcciones = clienteBd.ClienteDireccions.Select(d => d.Calle).ToList() 
        };
    }

    public async Task<LaCentral.UseCases.Entidades.Cliente?> ObtenerPorCuitAsync(string cuit, CancellationToken ct = default)
    {
        var c = await _context.Clientes.FirstOrDefaultAsync(x => x.CuitCuil == cuit, ct);
        if (c == null) return null;
        
        // Mapeo mínimo para el mensaje de error del CA-003
        return new LaCentral.UseCases.Entidades.Cliente 
        { 
            Codigo = c.Codigo, 
            RazonSocial = c.RazonSocial, 
            Cuit = c.CuitCuil 
        };
    }

    public async Task ActualizarAsync(LaCentral.UseCases.Entidades.Cliente cliente, CancellationToken ct = default)
    {
        // Traemos la entidad completa con tracking
        var clienteBd = await _context.Clientes
            .Include(c => c.ClienteTelefonos)
            .Include(c => c.ClienteDireccions)
            .SingleOrDefaultAsync(c => c.Codigo == cliente.Codigo, ct);

        if (clienteBd == null) return;

        // Actualización de primitivos
        clienteBd.RazonSocial = cliente.RazonSocial;
        clienteBd.CuitCuil = cliente.Cuit;
        clienteBd.CondicionFiscal = cliente.CondicionFiscal;
        clienteBd.CondicionPago = cliente.CondicionPago;
        clienteBd.Codigo = cliente.Codigo;
        clienteBd.Activo = cliente.Activo;
        clienteBd.MotivoBaja = cliente.MotivoBaja;
        clienteBd.FechaBaja = cliente.FechaBaja;

        // Sincronización inteligente de Teléfonos
        var telsABorrar = clienteBd.ClienteTelefonos.Where(t => !cliente.Telefonos.Contains(t.Numero)).ToList();
        foreach (var t in telsABorrar) _context.Remove(t);

        var telsNuevos = cliente.Telefonos.Where(t => !clienteBd.ClienteTelefonos.Any(bd => bd.Numero == t)).ToList();
        foreach (var t in telsNuevos) clienteBd.ClienteTelefonos.Add(new LaCentral.Data.Models.ClienteTelefono { Numero = t });

        // Sincronización inteligente de Direcciones
        var dirsABorrar = clienteBd.ClienteDireccions.Where(d => !cliente.Direcciones.Contains(d.Calle ?? string.Empty)).ToList();
        foreach (var d in dirsABorrar) _context.Remove(d);

        var dirsNuevas = cliente.Direcciones.Where(d => !clienteBd.ClienteDireccions.Any(bd => bd.Calle == d)).ToList();
        foreach (var d in dirsNuevas) clienteBd.ClienteDireccions.Add(new LaCentral.Data.Models.ClienteDireccion { Calle = d });

        await _context.SaveChangesAsync(ct);
    }
}
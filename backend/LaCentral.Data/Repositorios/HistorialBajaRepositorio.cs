using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Data.Repositorios;

/// <summary>
/// D10: las cuatro tablas *_historial_baja. Extraído de la lógica que antes vivía
/// inline en ClienteRepositorio y ProveedorRepositorio.ActualizarAsync.
/// </summary>
public class HistorialBajaRepositorio : IHistorialBajaRepositorio
{
    private readonly LaCentralDbContext _context;

    public HistorialBajaRepositorio(LaCentralDbContext context)
    {
        _context = context;
    }

    public async Task AbrirAsync(EntidadConHistorialBaja entidad, int entidadId, string motivo,
        DateTime fechaBaja, int usuarioBajaId, CancellationToken ct = default)
    {
        switch (entidad)
        {
            case EntidadConHistorialBaja.Cliente:
                _context.ClienteHistorialBaja.Add(new ClienteHistorialBaja
                    { ClienteId = entidadId, Motivo = motivo, FechaBaja = fechaBaja, UsuarioBajaId = usuarioBajaId });
                break;
            case EntidadConHistorialBaja.Proveedor:
                _context.ProveedorHistorialBaja.Add(new ProveedorHistorialBaja
                    { ProveedorId = entidadId, Motivo = motivo, FechaBaja = fechaBaja, UsuarioBajaId = usuarioBajaId });
                break;
            case EntidadConHistorialBaja.Articulo:
                _context.ArticuloHistorialBaja.Add(new ArticuloHistorialBaja
                    { ArticuloId = entidadId, Motivo = motivo, FechaBaja = fechaBaja, UsuarioBajaId = usuarioBajaId });
                break;
            case EntidadConHistorialBaja.Usuario:
                _context.UsuarioHistorialBaja.Add(new UsuarioHistorialBaja
                    { UsuarioId = entidadId, Motivo = motivo, FechaBaja = fechaBaja, UsuarioBajaId = usuarioBajaId });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entidad), entidad, null);
        }

        // Una segunda baja abierta la rechaza el índice único parcial (23505).
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> CerrarAsync(EntidadConHistorialBaja entidad, int entidadId,
        DateTime fechaReactivacion, int usuarioReactivacionId, CancellationToken ct = default)
    {
        // ExecuteUpdate corre dentro de la transacción abierta del contexto, si la hay.
        var filas = entidad switch
        {
            EntidadConHistorialBaja.Cliente => await _context.ClienteHistorialBaja
                .Where(h => h.ClienteId == entidadId && h.FechaReactivacion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.FechaReactivacion, fechaReactivacion)
                    .SetProperty(h => h.UsuarioReactivacionId, usuarioReactivacionId), ct),
            EntidadConHistorialBaja.Proveedor => await _context.ProveedorHistorialBaja
                .Where(h => h.ProveedorId == entidadId && h.FechaReactivacion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.FechaReactivacion, fechaReactivacion)
                    .SetProperty(h => h.UsuarioReactivacionId, usuarioReactivacionId), ct),
            EntidadConHistorialBaja.Articulo => await _context.ArticuloHistorialBaja
                .Where(h => h.ArticuloId == entidadId && h.FechaReactivacion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.FechaReactivacion, fechaReactivacion)
                    .SetProperty(h => h.UsuarioReactivacionId, usuarioReactivacionId), ct),
            EntidadConHistorialBaja.Usuario => await _context.UsuarioHistorialBaja
                .Where(h => h.UsuarioId == entidadId && h.FechaReactivacion == null)
                .ExecuteUpdateAsync(s => s.SetProperty(h => h.FechaReactivacion, fechaReactivacion)
                    .SetProperty(h => h.UsuarioReactivacionId, usuarioReactivacionId), ct),
            _ => throw new ArgumentOutOfRangeException(nameof(entidad), entidad, null)
        };

        return filas > 0;
    }

    public async Task<IReadOnlyList<RegistroBaja>> ObtenerAsync(EntidadConHistorialBaja entidad, int entidadId,
        CancellationToken ct = default)
    {
        var registros = entidad switch
        {
            EntidadConHistorialBaja.Cliente => _context.ClienteHistorialBaja.Where(h => h.ClienteId == entidadId)
                .Select(h => new RegistroBaja { Id = h.Id, Motivo = h.Motivo, FechaBaja = h.FechaBaja,
                    FechaReactivacion = h.FechaReactivacion, UsuarioBajaId = h.UsuarioBajaId, UsuarioReactivacionId = h.UsuarioReactivacionId }),
            EntidadConHistorialBaja.Proveedor => _context.ProveedorHistorialBaja.Where(h => h.ProveedorId == entidadId)
                .Select(h => new RegistroBaja { Id = h.Id, Motivo = h.Motivo, FechaBaja = h.FechaBaja,
                    FechaReactivacion = h.FechaReactivacion, UsuarioBajaId = h.UsuarioBajaId, UsuarioReactivacionId = h.UsuarioReactivacionId }),
            EntidadConHistorialBaja.Articulo => _context.ArticuloHistorialBaja.Where(h => h.ArticuloId == entidadId)
                .Select(h => new RegistroBaja { Id = h.Id, Motivo = h.Motivo, FechaBaja = h.FechaBaja,
                    FechaReactivacion = h.FechaReactivacion, UsuarioBajaId = h.UsuarioBajaId, UsuarioReactivacionId = h.UsuarioReactivacionId }),
            EntidadConHistorialBaja.Usuario => _context.UsuarioHistorialBaja.Where(h => h.UsuarioId == entidadId)
                .Select(h => new RegistroBaja { Id = h.Id, Motivo = h.Motivo, FechaBaja = h.FechaBaja,
                    FechaReactivacion = h.FechaReactivacion, UsuarioBajaId = h.UsuarioBajaId, UsuarioReactivacionId = h.UsuarioReactivacionId }),
            _ => throw new ArgumentOutOfRangeException(nameof(entidad), entidad, null)
        };

        return await registros.AsNoTracking().OrderBy(r => r.FechaBaja).ThenBy(r => r.Id).ToListAsync(ct);
    }
}

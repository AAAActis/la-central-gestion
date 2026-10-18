using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

public class DarDeBajaClienteUseCase
{
    private readonly IClienteRepositorio _repositorio;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public DarDeBajaClienteUseCase(IClienteRepositorio repositorio, IHistorialBajaRepositorio historial,
        IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _repositorio = repositorio;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public async Task<Result> EjecutarAsync(int id, string confirmacion, string motivo, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (cliente is null)
            return Result.Failure(TipoError.NoEncontrado, "El cliente no existe.");

        if (!cliente.Activo)
            return Result.Failure(TipoError.Invalido, "El cliente ya está inactivo.");
        
        string valorEsperado = string.IsNullOrWhiteSpace(cliente.Cuit) ? cliente.Codigo : cliente.Cuit;
        // CA-002: confirmación por reescritura
        if (valorEsperado != confirmacion)
            return Result.Failure(TipoError.Invalido, "El valor reescrito no coincide con el código o CUIT del cliente.");

        // CA-003: motivo obligatorio
        if (string.IsNullOrWhiteSpace(motivo))
            return Result.Failure(TipoError.Invalido, "El motivo de baja es obligatorio.");

        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // CA-001 y CA-004: aplicar baja lógica
        cliente.Activo = false;
        cliente.MotivoBaja = motivo;
        cliente.FechaBaja = DateTime.UtcNow;
        cliente.UsuarioBajaId = usuarioId;

        // D10: la fila y su historial, juntos o ninguno.
        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (!await _repositorio.ActualizarAsync(cliente, c))
                return Result.Failure(TipoError.NoEncontrado, "El cliente no existe.");

            await _historial.AbrirAsync(EntidadConHistorialBaja.Cliente, cliente.Id, motivo,
                cliente.FechaBaja.Value, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}
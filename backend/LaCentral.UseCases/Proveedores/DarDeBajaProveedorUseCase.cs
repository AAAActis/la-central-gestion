using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Proveedores;

public class DarDeBajaProveedorUseCase
{
    private readonly IProveedorRepositorio _repositorio;
    private readonly IHistorialBajaRepositorio _historial;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public DarDeBajaProveedorUseCase(IProveedorRepositorio repositorio, IHistorialBajaRepositorio historial,
        IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
    {
        _repositorio = repositorio;
        _historial = historial;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public async Task<Result> EjecutarAsync(int id, DarDeBajaProveedorRequest request, CancellationToken ct = default)
    {
        // CA-003: Motivo obligatorio
        if (string.IsNullOrWhiteSpace(request.MotivoBaja))
        {
            return Result.Failure(TipoError.Invalido, "Debe especificar un motivo para la baja del proveedor.");
        }

        var proveedor = await _repositorio.ObtenerDetallePorIdAsync(id, ct);
        
        if (proveedor is null)
        {
            return Result.Failure(TipoError.NoEncontrado, "El proveedor solicitado no existe.");
        }

        if (!proveedor.Activo)
        {
            return Result.Failure(TipoError.Invalido, "El proveedor ya se encuentra dado de baja.");
        }

        // CA-001 & CA-002: Baja lógica, asignación de motivo y reescritura de CUIT para liberar unicidad
        bool coincideCuit = !string.IsNullOrWhiteSpace(proveedor.Cuit) && proveedor.Cuit == request.Confirmacion;
        bool coincideCodigo = !string.IsNullOrWhiteSpace(proveedor.Codigo) && proveedor.Codigo == request.Confirmacion;
        
        if (!coincideCuit && !coincideCodigo)
        {
            return Result.Failure(TipoError.Invalido, "El valor ingresado no coincide con el CUIT ni con el Código del proveedor.");
        }
        
        // El usuario sale de la sesión (0 = sin identidad): queda en el historial D10.
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        proveedor.Activo = false;
        proveedor.MotivoBaja = request.MotivoBaja;
        proveedor.FechaBaja = DateTime.UtcNow;

        // D10: la fila y su historial, juntos o ninguno.
        return await _unidadDeTrabajo.EjecutarAsync(async c =>
        {
            if (!await _repositorio.ActualizarAsync(proveedor, c))
                return Result.Failure(TipoError.NoEncontrado, "El proveedor solicitado no existe.");

            await _historial.AbrirAsync(EntidadConHistorialBaja.Proveedor, proveedor.Id, request.MotivoBaja,
                proveedor.FechaBaja.Value, usuarioId, c);
            return Result.Success();
        }, ct);
    }
}
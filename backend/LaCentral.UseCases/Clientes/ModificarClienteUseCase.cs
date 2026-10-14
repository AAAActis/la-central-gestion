using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

public class ModificarClienteUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;

    public ModificarClienteUseCase(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
    }

    public async Task<Result> EjecutarAsync(int id, ModificarClienteRequest request, CancellationToken ct = default)
    {
        // 1. Verificar existencia
        var cliente = await _clienteRepositorio.ObtenerDetallePorIdAsync(id, ct);
        if (cliente == null)
            return Result.Failure(TipoError.NoEncontrado, "El cliente indicado no existe.");

        // 2. CA-005: Bloquear si está dado de baja
        if (!cliente.Activo)
            return Result.Failure(TipoError.Invalido, "El cliente está dado de baja. Reactívelo antes de modificarlo.");

        // 3. CA-002: Reutilizar validaciones centralizadas
        var validacion = ClienteValidaciones.ValidarLimites(
            request.Codigo, request.RazonSocial, request.Cuit, request.CondicionFiscal, request.CondicionPago);
        
        if (!validacion.IsSuccess) return validacion;

        // CRÍTICO 3: el código nuevo no puede ser el de otro cliente. El índice
        // único cliente_codigo_key lo garantiza ante dos modificaciones simultáneas.
        if (request.Codigo != cliente.Codigo && await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
            return Result.Failure(TipoError.Conflicto, $"El código {request.Codigo} ya pertenece a otro cliente.");

        // 4. CA-003: CUIT duplicado informa a quién pertenece
        if (!string.IsNullOrWhiteSpace(request.Cuit) && request.Cuit != cliente.Cuit)
        {
            var clienteExistente = await _clienteRepositorio.ObtenerPorCuitAsync(request.Cuit, ct);
            if (clienteExistente != null && clienteExistente.Codigo != cliente.Codigo)
            {
                return Result.Failure(TipoError.Conflicto, $"El CUIT/CUIL ingresado ya pertenece al cliente: {clienteExistente.RazonSocial} ({clienteExistente.Codigo}).");
            }
        }

        // 5. Actualizar entidad de dominio (las colecciones asumen vacío si llegan null)
        cliente.Codigo = request.Codigo;
        cliente.RazonSocial = request.RazonSocial;
        cliente.Cuit = request.Cuit;
        cliente.CondicionFiscal = request.CondicionFiscal;
        cliente.CondicionPago = request.CondicionPago;
        cliente.Telefonos = (request.Telefonos ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        // Una dirección sin calle viaja como "": se conserva para no borrar su localidad.
        cliente.Direcciones = (request.Direcciones ?? new List<string>()).Select(d => d?.Trim() ?? string.Empty).ToList();

        if (!await _clienteRepositorio.ActualizarAsync(cliente, ct))
            return Result.Failure(TipoError.NoEncontrado, "El cliente indicado no existe.");

        return Result.Success();
    }
}
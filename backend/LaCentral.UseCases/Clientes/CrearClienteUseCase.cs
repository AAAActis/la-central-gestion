using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

public class CrearClienteUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;
    private readonly IContextoUsuario _contextoUsuario;

    public CrearClienteUseCase(
        IClienteRepositorio clienteRepositorio, 
        IContextoUsuario contextoUsuario)
    {
        _clienteRepositorio = clienteRepositorio;
        _contextoUsuario = contextoUsuario;
    }

    public async Task<Result<CrearClienteResponse>> EjecutarAsync(CrearClienteRequest request, CancellationToken ct = default)
    {
        // 1. Validación centralizada (Hallazgo 3 reutilizado)
        var validacion = ClienteValidaciones.ValidarLimites(
            request.Codigo, 
            request.RazonSocial, 
            request.Cuit, 
            request.CondicionFiscal, 
            request.CondicionPago
        );

        if (!validacion.IsSuccess)
        {
            return Result<CrearClienteResponse>.Failure(validacion.Tipo, validacion.Error);
        }

        // 2. CA-001: Código duplicado
        // El índice cliente_codigo_key garantiza la unicidad ante dos altas simultáneas.
        var duenioDelCodigo = await _clienteRepositorio.ObtenerPorCodigoAsync(request.Codigo, ct);
        if (duenioDelCodigo is not null)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto,
                $"El código {request.Codigo} ya pertenece al cliente: {duenioDelCodigo.RazonSocial}.");
        }

        // 3. CUIT duplicado
        if (!string.IsNullOrWhiteSpace(request.Cuit) && await _clienteRepositorio.ExisteCuitAsync(request.Cuit, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este CUIT/CUIL.");
        }

        // 4. CA-002: Razón social repetida
        string? advertencia = null;
        if (await _clienteRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro cliente con la misma Razón Social.";
        }

        // 5. Hallazgo 4: Mapeo tolerante a nulos usando ?? new List<string>()
        var nuevoCliente = new Cliente
        {
            Codigo = request.Codigo,
            RazonSocial = request.RazonSocial,
            Cuit = request.Cuit,
            CondicionFiscal = request.CondicionFiscal,
            CondicionPago = request.CondicionPago,
            Telefonos = (request.Telefonos ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
            Direcciones = (request.Direcciones ?? new List<string>()).Where(d => !string.IsNullOrWhiteSpace(d)).ToList(),
        };

        await _clienteRepositorio.AgregarAsync(nuevoCliente, ct);
        
        var response = new CrearClienteResponse(nuevoCliente.Codigo, advertencia);
        return Result<CrearClienteResponse>.Success(response);
    }
}
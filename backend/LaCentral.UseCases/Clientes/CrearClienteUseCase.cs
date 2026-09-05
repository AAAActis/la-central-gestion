using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

public class CrearClienteUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;
    private readonly IContextoUsuario _contextoUsuario;

    // Inyectamos el contexto de usuario para la trazabilidad
    public CrearClienteUseCase(
        IClienteRepositorio clienteRepositorio, 
        IContextoUsuario contextoUsuario)
    {
        _clienteRepositorio = clienteRepositorio;
        _contextoUsuario = contextoUsuario;
    }

    public async Task<Result<CrearClienteResponse>> EjecutarAsync(CrearClienteRequest request, CancellationToken ct = default)
    {
        // 1. Validar campos obligatorios básicos, condición fiscal y condición de pago
        if (string.IsNullOrWhiteSpace(request.Codigo) || 
            string.IsNullOrWhiteSpace(request.RazonSocial) || 
            string.IsNullOrWhiteSpace(request.CondicionFiscal) || 
            string.IsNullOrWhiteSpace(request.CondicionPago))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "El código, la razón social, la condición fiscal y la condición de pago son obligatorios.");
        }

        // 2. CA-001: Código duplicado (Conflicto 409)
        if (await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este código.");
        }

        // 2.bis: CUIT duplicado (Evita el error 500 por índice único en la base)
        if (!string.IsNullOrWhiteSpace(request.Cuit) && await _clienteRepositorio.ExisteCuitAsync(request.Cuit, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este CUIT/CUIL.");
        }

        // 3. CA-002: Razón social repetida -> Genera advertencia
        string? advertencia = null;
        if (await _clienteRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro cliente con la misma Razón Social.";
        }

        // 4. CA-004: Mapeo de la entidad (filtrando strings vacíos en listas)
        var nuevoCliente = new Cliente
        {
            Codigo = request.Codigo,
            RazonSocial = request.RazonSocial,
            Cuit = request.Cuit,
            CondicionFiscal = request.CondicionFiscal,
            CondicionPago = request.CondicionPago,
            Telefonos = request.Telefonos.Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
            Direcciones = request.Direcciones.Where(d => !string.IsNullOrWhiteSpace(d)).ToList(),
        };

        // Persistimos
        await _clienteRepositorio.AgregarAsync(nuevoCliente, ct);

        // Armamos la respuesta
        var response = new CrearClienteResponse(nuevoCliente.Codigo, advertencia);
        
        return Result<CrearClienteResponse>.Success(response);
    }
}
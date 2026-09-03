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
        // 1. CA-003: Código y razón social obligatorios (Agregado TipoError.Invalido)
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "El código y la razón social son obligatorios.");
        }

        // 2. CA-001: Código duplicado (Agregado TipoError.Conflicto para el HTTP 409)
        if (await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este código.");
        }

        // 3. CA-002: Razón social repetida -> Genera advertencia
        string? advertencia = null;
        if (await _clienteRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro cliente con la misma Razón Social.";
        }

        // 4. CA-004: Mapeo de la entidad
        var nuevoCliente = new Cliente
        {
            Codigo = request.Codigo,
            RazonSocial = request.RazonSocial,
            Cuit = request.Cuit,
            
            // TODO para Santi: instanciar listas y usar _contextoUsuario.UsuarioId para trazabilidad
        };

        // Persistimos
        await _clienteRepositorio.AgregarAsync(nuevoCliente, ct);

        // Armamos la respuesta
        var response = new CrearClienteResponse(nuevoCliente.Codigo, advertencia);
        
        return Result<CrearClienteResponse>.Success(response);
    }
}
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun; // Ajustá según donde esté tu entidad Cliente

namespace LaCentral.UseCases.Clientes;

public class CrearClienteUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;

    public CrearClienteUseCase(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
    }

    public async Task<Result<CrearClienteResponse>> EjecutarAsync(CrearClienteRequest request, CancellationToken ct = default)
    {
        // 1. CA-003: Código y razón social obligatorios
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            return Result<CrearClienteResponse>.Failure("El código y la razón social son obligatorios.");
        }

        // 2. CA-001: Código duplicado (Hard Key)
        if (await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
        {
            return Result<CrearClienteResponse>.Failure("Conflicto: Ya existe un cliente registrado con este código.");
        }

        // 3. CA-002: Razón social repetida -> Genera advertencia, pero NO frena el flujo
        string? advertencia = null;
        if (await _clienteRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro cliente con la misma Razón Social.";
        }

        // 4. CA-004: Mapeo de la entidad (CUIT opcional y colecciones)
        var nuevoCliente = new Cliente
        {
            Codigo = request.Codigo,
            RazonSocial = request.RazonSocial,
            Cuit = request.Cuit, // Pasa nulo sin problemas si no lo envían
            
            // TODO para Santi: instanciar las entidades hijas según cómo las haya armado en EF Core
            // Telefonos = request.Telefonos.Select(t => ...).ToList(),
            // Direcciones = request.Direcciones.Select(d => ...).ToList()
        };

        // Persistimos
        await _clienteRepositorio.AgregarAsync(nuevoCliente, ct);

        // Armamos la respuesta incluyendo la advertencia (que será null si no hubo duplicados)
        var response = new CrearClienteResponse(nuevoCliente.Codigo, advertencia);
        
        return Result<CrearClienteResponse>.Success(response);
    }
}


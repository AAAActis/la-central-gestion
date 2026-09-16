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
        // 1. Hallazgo 3: Validación de campos obligatorios y sus longitudes máximas
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Length > 20 || 
            string.IsNullOrWhiteSpace(request.RazonSocial) || request.RazonSocial.Length > 120 || 
            string.IsNullOrWhiteSpace(request.CondicionFiscal) || request.CondicionFiscal.Length > 30 || 
            string.IsNullOrWhiteSpace(request.CondicionPago) || request.CondicionPago.Length > 60)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "Faltan campos obligatorios o superan la longitud máxima permitida en la base de datos.");
        }

        // Validación de longitud de CUIT
        if (!string.IsNullOrWhiteSpace(request.Cuit) && request.Cuit.Length > 13)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "El CUIT/CUIL no puede superar los 13 caracteres.");
        }

        // 2. CA-001: Código duplicado
        if (await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este código.");
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
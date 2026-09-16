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
        // 1. Hallazgo 4: Rechazo explícito de colecciones nulas (Falla rápido, devuelve 400)
        if (request.Telefonos == null || request.Direcciones == null)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "Las listas de teléfonos y direcciones no pueden ser nulas. Enviá un arreglo vacío [] en su lugar.");
        }

        // 2. Hallazgo 3: Validación de campos obligatorios y sus longitudes máximas
        if (string.IsNullOrWhiteSpace(request.Codigo) || request.Codigo.Length > 20 || 
            string.IsNullOrWhiteSpace(request.RazonSocial) || request.RazonSocial.Length > 120 || 
            string.IsNullOrWhiteSpace(request.CondicionFiscal) || request.CondicionFiscal.Length > 30 || 
            string.IsNullOrWhiteSpace(request.CondicionPago) || request.CondicionPago.Length > 60)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "Faltan campos obligatorios o superan la longitud máxima permitida en la base de datos.");
        }

        // Validación de longitud de CUIT (si viene con datos)
        if (!string.IsNullOrWhiteSpace(request.Cuit) && request.Cuit.Length > 13)
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Invalido, "El CUIT/CUIL no puede superar los 13 caracteres.");
        }

        // 3. CA-001: Código duplicado (Conflicto 409)
        if (await _clienteRepositorio.ExisteCodigoAsync(request.Codigo, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este código.");
        }

        // 4. CUIT duplicado (Evita el error 500 por índice único en la base)
        if (!string.IsNullOrWhiteSpace(request.Cuit) && await _clienteRepositorio.ExisteCuitAsync(request.Cuit, ct))
        {
            return Result<CrearClienteResponse>.Failure(TipoError.Conflicto, "Ya existe un cliente registrado con este CUIT/CUIL.");
        }

        // 5. CA-002: Razón social repetida -> Genera advertencia
        string? advertencia = null;
        if (await _clienteRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro cliente con la misma Razón Social.";
        }

        // 6. Mapeo de la entidad (Se asume que ya no son nulas gracias a la validación superior)
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
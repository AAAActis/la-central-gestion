using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

public class ObtenerClienteDetalleUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;

    public ObtenerClienteDetalleUseCase(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
    }

    public async Task<Result<ClienteDetalleDto>> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await _clienteRepositorio.ObtenerDetallePorIdAsync(id, ct);

        if (cliente == null)
        {
            return Result<ClienteDetalleDto>.Failure(TipoError.NoEncontrado, "El cliente indicado no existe.");
        }

        var dto = new ClienteDetalleDto(
            cliente.Codigo,
            cliente.RazonSocial,
            cliente.Cuit,
            cliente.CondicionFiscal,
            cliente.CondicionPago,
            cliente.Activo,
            cliente.Telefonos ?? new List<string>(),
            cliente.Direcciones ?? new List<string>()
        );

        return Result<ClienteDetalleDto>.Success(dto);
    }
}
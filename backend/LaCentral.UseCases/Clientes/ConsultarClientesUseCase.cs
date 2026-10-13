using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Clientes;

/// <summary>HU-CLI-02: búsqueda de clientes por Razón Social o CUIT/CUIL.</summary>
public class ConsultarClientesUseCase
{
    private readonly IClienteRepositorio _clienteRepositorio;

    public ConsultarClientesUseCase(IClienteRepositorio clienteRepositorio)
    {
        _clienteRepositorio = clienteRepositorio;
    }

    public async Task<Result<IReadOnlyList<ClienteResumenDto>>> EjecutarAsync(
        string texto, bool incluirInactivos, CancellationToken ct = default)
    {
        // CA: hace falta un criterio de búsqueda, no se lista todo el padrón.
        if (string.IsNullOrWhiteSpace(texto))
        {
            return Result<IReadOnlyList<ClienteResumenDto>>.Failure(
                TipoError.Invalido, "Ingresá una razón social o un CUIT/CUIL para buscar.");
        }

        var clientes = await _clienteRepositorio.BuscarAsync(texto.Trim(), incluirInactivos, ct);

        // CA: sin resultados hay que avisarlo explícitamente, no devolver una lista vacía sin más.
        if (clientes.Count == 0)
        {
            return Result<IReadOnlyList<ClienteResumenDto>>.Failure(
                TipoError.NoEncontrado, "No se encontraron clientes que coincidan con la búsqueda.");
        }

        var respuesta = clientes
            .Select(c => new ClienteResumenDto(
                c.Codigo, c.RazonSocial, c.Cuit, c.CondicionFiscal, c.CondicionPago, c.Activo))
            .ToList();

        return Result<IReadOnlyList<ClienteResumenDto>>.Success(respuesta);
    }
}

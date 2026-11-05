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

    public async Task<Result<PaginaDto<ClienteResumenDto>>> EjecutarAsync(
        string texto, bool incluirInactivos, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        // CA: hace falta un criterio de búsqueda, no se lista todo el padrón.
        if (string.IsNullOrWhiteSpace(texto))
        {
            return Result<PaginaDto<ClienteResumenDto>>.Failure(
                TipoError.Invalido, "Ingresá una razón social o un CUIT/CUIL para buscar.");
        }

        if (pagina < 1 || tamanoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanoPagina > int.MaxValue)
        {
            return Result<PaginaDto<ClienteResumenDto>>.Failure(TipoError.Invalido, "La página o su tamaño no son válidos.");
        }

        // CA-003 v1.1 (#147): sin coincidencias es una página vacía, no un 404.
        var (lista, total) = await _clienteRepositorio.BuscarAsync(texto.Trim(), incluirInactivos, pagina, tamanoPagina, ct);

        var respuesta = lista
            .Select(c => new ClienteResumenDto(
                c.Id, c.Codigo, c.RazonSocial, c.Cuit, c.CondicionFiscal, c.CondicionPago, c.Activo))
            .ToList();

        return Result<PaginaDto<ClienteResumenDto>>.Success(new PaginaDto<ClienteResumenDto>(respuesta, total, pagina, tamanoPagina));
    }
}

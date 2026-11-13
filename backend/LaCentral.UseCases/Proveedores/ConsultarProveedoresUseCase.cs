using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Proveedores;

/// <summary>HU-PRO-02: búsqueda de proveedores por Razón Social o CUIT. Mismo patrón que ConsultarClientesUseCase.</summary>
public class ConsultarProveedoresUseCase
{
    private readonly IProveedorRepositorio _proveedorRepositorio;

    public ConsultarProveedoresUseCase(IProveedorRepositorio proveedorRepositorio)
    {
        _proveedorRepositorio = proveedorRepositorio;
    }

    public async Task<Result<PaginaDto<ProveedorResumenDto>>> EjecutarAsync(
        string texto, bool incluirInactivos, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default,
        bool porCuit = true)
    {
        // CA-003: hace falta un criterio de búsqueda, no se lista todo el padrón.
        if (string.IsNullOrWhiteSpace(texto))
        {
            return Result<PaginaDto<ProveedorResumenDto>>.Failure(
                TipoError.Invalido, "Ingresá una razón social o un CUIT para buscar.");
        }

        if (pagina < 1 || tamanoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanoPagina > int.MaxValue)
        {
            return Result<PaginaDto<ProveedorResumenDto>>.Failure(TipoError.Invalido, "La página o su tamaño no son válidos.");
        }

        // CA-003 v1.1 (#147): sin coincidencias es una página vacía, no un 404.
        var (lista, total) = await _proveedorRepositorio.BuscarAsync(texto.Trim(), incluirInactivos, pagina, tamanoPagina, porCuit, ct);

        var respuesta = lista
            .Select(p => new ProveedorResumenDto(p.Id, p.Codigo, p.RazonSocial, p.Cuit, p.UrlReferencia, p.Activo))
            .ToList();

        return Result<PaginaDto<ProveedorResumenDto>>.Success(new PaginaDto<ProveedorResumenDto>(respuesta, total, pagina, tamanoPagina));
    }
}

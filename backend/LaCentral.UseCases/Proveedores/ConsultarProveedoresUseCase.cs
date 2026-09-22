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

    public async Task<Result<IReadOnlyList<ProveedorResumenDto>>> EjecutarAsync(
        string texto, bool incluirInactivos, CancellationToken ct = default)
    {
        // CA-003: hace falta un criterio de búsqueda, no se lista todo el padrón.
        if (string.IsNullOrWhiteSpace(texto))
        {
            return Result<IReadOnlyList<ProveedorResumenDto>>.Failure(
                TipoError.Invalido, "Ingresá una razón social o un CUIT para buscar.");
        }

        var proveedores = await _proveedorRepositorio.BuscarAsync(texto.Trim(), incluirInactivos, ct);

        // CA-003: sin resultados hay que avisarlo explícitamente, no devolver una lista vacía sin más.
        if (proveedores.Count == 0)
        {
            return Result<IReadOnlyList<ProveedorResumenDto>>.Failure(
                TipoError.NoEncontrado, "No se encontraron proveedores que coincidan con la búsqueda.");
        }

        var respuesta = proveedores
            .Select(p => new ProveedorResumenDto(p.Id, p.RazonSocial, p.Cuit, p.UrlReferencia, p.Activo))
            .ToList();

        return Result<IReadOnlyList<ProveedorResumenDto>>.Success(respuesta);
    }
}

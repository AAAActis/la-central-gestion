using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-01 · Alta de artículo.</summary>
public class CrearArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public CrearArticuloUseCase(
        IArticuloRepositorio articulos,
        IConsultaArticulosRepositorio consulta,
        IProveedorRepositorio proveedores,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _articulos = articulos;
        _consulta = consulta;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    public Task<Result<CrearArticuloResponse>> EjecutarAsync(CrearArticuloRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<CrearArticuloResponse>.Success(new(0, string.Empty)));
}

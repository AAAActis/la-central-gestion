using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-03 · Modificación de artículo y alta de códigos de proveedor.</summary>
public class ModificarArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ModificarArticuloUseCase(
        IArticuloRepositorio articulos,
        IConsultaArticulosRepositorio consulta,
        IProveedorRepositorio proveedores,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
    {
        _articulos = articulos;
        _consulta = consulta;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public Task<Result> EjecutarAsync(int id, ModificarArticuloRequest request, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

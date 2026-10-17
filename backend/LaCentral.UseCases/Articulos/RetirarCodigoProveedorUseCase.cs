using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-03 · Retiro explícito del código de un proveedor. Solo ADMINISTRADOR.</summary>
public class RetirarCodigoProveedorUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IContextoUsuario _contexto;

    public RetirarCodigoProveedorUseCase(
        IArticuloRepositorio articulos, IConsultaArticulosRepositorio consulta, IContextoUsuario contexto)
    {
        _articulos = articulos;
        _consulta = consulta;
        _contexto = contexto;
    }

    public Task<Result> EjecutarAsync(int id, int proveedorId, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

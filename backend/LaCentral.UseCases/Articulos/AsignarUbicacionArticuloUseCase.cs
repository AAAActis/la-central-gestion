using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>
/// N-9 · La ubicación en depósito es de cada sucursal y se carga desde la ficha, no en el
/// alta: el estante se asigna cuando la mercadería llega (decisión del 04/11/2026).
/// ADMINISTRADOR y OPERADOR. Vacío borra la ubicación.
/// </summary>
public class AsignarUbicacionArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;

    public AsignarUbicacionArticuloUseCase(IArticuloRepositorio articulos, IConsultaArticulosRepositorio consulta)
    {
        _articulos = articulos;
        _consulta = consulta;
    }

    public Task<Result> EjecutarAsync(int id, short sucursalId, string? ubicacionDeposito, CancellationToken ct = default)
        => throw new NotImplementedException();
}

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

    private const string MensajeDadoDeBaja = "El artículo está dado de baja: reactivalo antes de modificarlo.";

    public async Task<Result> EjecutarAsync(int id, short sucursalId, string? ubicacionDeposito, CancellationToken ct = default)
    {
        var validacion = ArticuloValidaciones.ValidarUbicacion(ubicacionDeposito);
        if (!validacion.IsSuccess)
            return validacion;

        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (!articulo.Activo)
            return Result.Failure(TipoError.Invalido, MensajeDadoDeBaja);
        if (!(await _articulos.ObtenerSucursalesAsync(ct)).Contains(sucursalId))
            return Result.Failure(TipoError.NoEncontrado, "La sucursal indicada no existe.");

        var ubicacion = string.IsNullOrWhiteSpace(ubicacionDeposito) ? null : ubicacionDeposito.Trim();
        // La condición "activo" va en la escritura: otra sesión pudo darlo de baja.
        return await _articulos.AsignarUbicacionAsync(id, sucursalId, ubicacion, ct)
            ? Result.Success()
            : Result.Failure(TipoError.Invalido, MensajeDadoDeBaja);
    }
}

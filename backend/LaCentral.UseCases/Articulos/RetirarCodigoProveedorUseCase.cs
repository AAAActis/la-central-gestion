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

    /// <summary>
    /// D2: hay un solo código por proveedor, así que el proveedor lo identifica. El rol sale
    /// de la sesión, nunca del cuerpo de la petición.
    /// </summary>
    public async Task<Result> EjecutarAsync(int id, int proveedorId, CancellationToken ct = default)
    {
        if (!_contexto.EsAdministrador)
            return Result.Failure(TipoError.NoAutorizado, "Solo un administrador puede retirar un código de proveedor.");

        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (!articulo.Activo)
            return Result.Failure(TipoError.Invalido, "El artículo está dado de baja: reactivalo antes de modificarlo.");
        if (!articulo.Codigos.Any(c => c.ProveedorId == proveedorId))
            return Result.Failure(TipoError.NoEncontrado, "El artículo no tiene un código de ese proveedor.");

        if (!await _articulos.RetirarCodigoAsync(id, proveedorId, ct))
            return Result.Failure(TipoError.NoEncontrado, "El artículo no tiene un código de ese proveedor.");
        return Result.Success();
    }
}

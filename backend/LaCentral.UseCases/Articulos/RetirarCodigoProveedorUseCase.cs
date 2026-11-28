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
    /// D2 revisada (D-S7-06, D-S7-19): un artículo puede tener varios códigos del mismo proveedor,
    /// así que el código se identifica por (proveedor, código). El rol sale de la sesión, nunca del
    /// cuerpo de la petición.
    /// </summary>
    public async Task<Result> EjecutarAsync(int id, int proveedorId, string? codigo, CancellationToken ct = default)
    {
        if (!_contexto.EsAdministrador)
            return Result.Failure(TipoError.Prohibido, "Solo un administrador puede retirar un código de proveedor.");

        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (!articulo.Activo)
            return Result.Failure(TipoError.Invalido, "El artículo está dado de baja: reactivalo antes de modificarlo.");
        if (string.IsNullOrWhiteSpace(codigo))
            return Result.Failure(TipoError.Invalido, "Indicá qué código del proveedor se retira.");
        var clave = NormalizacionArticulo.Codigo(codigo);
        if (!articulo.Codigos.Any(c => c.ProveedorId == proveedorId && c.ClaveNormalizada == clave))
            return Result.Failure(TipoError.NoEncontrado, $"El artículo no tiene el código {clave} de ese proveedor.");

        if (!await _articulos.RetirarCodigoAsync(id, proveedorId, clave, ct))
            return Result.Failure(TipoError.NoEncontrado, $"El artículo no tiene el código {clave} de ese proveedor.");
        return Result.Success();
    }
}

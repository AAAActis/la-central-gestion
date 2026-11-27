using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-012 · Anulación de la nota de crédito, como la de la factura (028): un ADMINISTRADOR solo (D-S7-04).</summary>
public class AnularNotaCreditoCompraUseCase
{
    private readonly INotaCreditoCompraRepositorio _notas;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public AnularNotaCreditoCompraUseCase(INotaCreditoCompraRepositorio notas, IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
        => (_notas, _unidadDeTrabajo, _contexto) = (notas, unidadDeTrabajo, contexto);

    public async Task<Result> EjecutarAsync(int notaId, AnularNotaCreditoCompraRequest request, CancellationToken ct = default)
    {
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");
        if (!_contexto.EsAdministrador)
            return Result.Failure(TipoError.Prohibido, "Solo un ADMINISTRADOR puede anular una nota de crédito.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure(TipoError.Invalido, "El motivo de la anulación es obligatorio.");

        var nota = await _notas.ObtenerParaAnularAsync(notaId, ct);
        if (nota is null)
            return Result.Failure(TipoError.NoEncontrado, "La nota de crédito indicada no existe.");
        if (!nota.Activa)
            return Result.Failure(TipoError.Conflicto, $"La nota de crédito {nota.Numero} ya está anulada.");

        // Si era por devolución, el repositorio devuelve el stock a cada origen en la misma transacción.
        var anulacion = new AnulacionNotaCreditoCompra(notaId, request.Motivo.Trim(), usuarioId, DateTime.UtcNow);
        return await _unidadDeTrabajo.EjecutarAsync(token => _notas.AnularConEfectosAsync(anulacion, token), ct);
    }
}

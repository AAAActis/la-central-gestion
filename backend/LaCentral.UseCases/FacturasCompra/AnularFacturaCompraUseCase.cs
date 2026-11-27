using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-04 (028) · Anulación de factura de compra: la hace un ADMINISTRADOR solo (D-S7-04).</summary>
public class AnularFacturaCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;
    private readonly CalcularPreciosArticuloUseCase _calcularPrecios;
    private readonly INotaCreditoCompraRepositorio _notas;

    public AnularFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto,
        CalcularPreciosArticuloUseCase calcularPrecios,
        INotaCreditoCompraRepositorio notas)
    {
        _notas = notas;
        _facturas = facturas;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
        _calcularPrecios = calcularPrecios;
    }

    public async Task<Result> EjecutarAsync(int facturaId, AnularFacturaCompraRequest request, CancellationToken ct = default)
    {
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");
        // CA-007 (v1.2, D-S7-04): solo un ADMINISTRADOR anula.
        if (!_contexto.EsAdministrador)
            return Result.Failure(TipoError.Prohibido, "Solo un ADMINISTRADOR puede anular una factura de compra.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure(TipoError.Invalido, "El motivo de la anulación es obligatorio.");

        var factura = await _facturas.ObtenerParaAnularAsync(facturaId, ct);
        if (factura is null)
            return Result.Failure(TipoError.NoEncontrado, "La factura de compra indicada no existe.");
        if (!factura.Activa)
            return Result.Failure(TipoError.Conflicto, $"La factura {factura.Numero} ya está anulada.");

        var anulacion = new AnulacionFacturaCompra(facturaId, request.Motivo.Trim(), usuarioId, DateTime.UtcNow);
        return await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            // CA-011 (D-S6-22): con la factura bloqueada, así no entra una nota entre la consulta y la anulación.
            var notas = await _notas.ObtenerVigentesDeFacturaAsync(facturaId, token);
            if (notas.Count > 0)
                return Result.Failure(TipoError.Conflicto,
                    $"La factura {factura.Numero} tiene notas de crédito vigentes ({string.Join(", ", notas)}): primero se anulan las notas y después la factura.");
            var anulada = await _facturas.AnularConEfectosAsync(anulacion, token);
            if (!anulada.IsSuccess)
                return anulada;
            // CA-001: los precios por modalidad siguen al costo devuelto, en la misma unidad de trabajo.
            return await _calcularPrecios.EjecutarAsync(factura.ArticuloIds, anulacion.Fecha, token);
        }, ct);
    }
}

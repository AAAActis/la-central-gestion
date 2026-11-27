namespace LaCentral.UseCases.FacturasCompra;

/// <summary>Un registro válido (no anulado) de articulo_historial_compra.</summary>
public sealed record RegistroCompraValido(decimal? PrecioCosto, int ProveedorId);

/// <summary>
/// Lo que la base sabe de un artículo de la factura que se anula. <c>CostoPrevioGuardado</c>: la
/// primera línea de ese artículo se registró con el 13 y guardó <c>CostoPrevio</c> (null = no tenía
/// costo). <c>CostoPuestoPorLaFactura</c>: el costo que dejó la factura (su fila del historial).
/// <c>AnteriorValido</c>: el último registro válido del historial sin contar esta factura.
/// </summary>
public sealed record CostoDeArticuloAlAnular(
    decimal? CostoActual, decimal? CostoPuestoPorLaFactura, bool CostoPrevioGuardado, decimal? CostoPrevio,
    RegistroCompraValido? AnteriorValido);

/// <summary>Qué escribir en el artículo. <c>CambiaCosto</c> false: el costo queda como está.</summary>
public sealed record EfectoEnArticulo(bool CambiaCosto, decimal? Costo, int? UltimoProveedorId);

/// <summary>028 CA-002 (v1.2) y 016 CA-004 (v1.1) · D-S7-03: el costo de un artículo al anular su factura.</summary>
public static class CostoAlAnular
{
    public static EfectoEnArticulo Decidir(CostoDeArticuloAlAnular a)
        => new(false, a.CostoActual, null);
}

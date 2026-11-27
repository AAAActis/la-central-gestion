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
    {
        // Sin registro anterior válido, el artículo queda sin último proveedor comprado (en los dos casos).
        var ultimoProveedor = a.AnteriorValido?.ProveedorId;
        if (a.CostoPrevioGuardado)
            // Si el costo sigue siendo el que puso la factura, nadie lo cambió: vuelve el de antes (null incluido).
            // shortcut: un cambio a mano al MISMO valor no se distingue de «nadie lo tocó»; haría falta un historial de costos.
            return a.CostoActual == a.CostoPuestoPorLaFactura
                ? new(true, a.CostoPrevio, ultimoProveedor)
                : new(false, a.CostoActual, ultimoProveedor);
        // Líneas anteriores al 13 (v1.1): el último registro válido; sin ninguno, conserva el costo.
        return a.AnteriorValido is null
            ? new(false, a.CostoActual, null)
            : new(true, a.AnteriorValido.PrecioCosto, ultimoProveedor);
    }
}

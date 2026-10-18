using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// El costo que deja una línea es el precio unitario neto de su descuento, redondeado a
/// centavos como articulo.precio_costo (NUMERIC(14,2)), con el mismo criterio de
/// PostgreSQL: el medio centavo se aleja del cero.
/// </summary>
public class FacturaCompraDetalleTests
{
    [Theory]
    [InlineData(200, 10, 180)]
    [InlineData(100, 0, 100)]
    [InlineData(100, 100, 0)]
    [InlineData(99.99, 33.33, 66.66)]   // 66,663333 → 66,66
    [InlineData(0.15, 50, 0.08)]        // 0,075 → 0,08: el medio centavo sube
    [InlineData(1.05, 50, 0.53)]        // 0,525 → 0,53 (bancario daría 0,52)
    public void CostoUnitario_EsElPrecioNetoDelDescuentoEnCentavos(double precio, double descuento, double esperado)
    {
        var linea = new FacturaCompraDetalle { PrecioUnitario = (decimal)precio, DescuentoPorcentaje = (decimal)descuento };

        Assert.Equal((decimal)esperado, linea.CostoUnitario);
    }
}

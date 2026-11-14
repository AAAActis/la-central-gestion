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

    // HU-FCO-06: los tres descuentos son sucesivos y se redondea una sola vez, al final.
    [Theory]
    [InlineData(100, 10, 5, 0, 85.5)]
    [InlineData(100, 10, 10, 10, 72.9)]
    [InlineData(200, 0, 0, 50, 100)]
    [InlineData(1.05, 0, 50, 0, 0.53)]
    [InlineData(99.99, 33.33, 33.33, 33.33, 29.63)]   // 29,6311… → 29,63
    public void CostoUnitario_ConTresDescuentosSucesivos(double precio, double d1, double d2, double d3, double esperado)
    {
        var linea = new FacturaCompraDetalle
        {
            PrecioUnitario = (decimal)precio, DescuentoPorcentaje = (decimal)d1,
            Descuento2Porcentaje = (decimal)d2, Descuento3Porcentaje = (decimal)d3
        };

        Assert.Equal((decimal)esperado, linea.CostoUnitario);
    }
}

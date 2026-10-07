using Xunit;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Entidades;

public class ArticuloTests
{
    // CA3: Sin costo vigente, no hay precio estimado
    [Fact]
    public void PrecioVentaEstimado_SinCosto_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = null, MargenGanancia = 20m };
        Assert.Null(articulo.PrecioVentaEstimado);
    }

    // CA3: Sin margen, no hay precio estimado
    [Fact]
    public void PrecioVentaEstimado_SinMargen_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m, MargenGanancia = null };
        Assert.Null(articulo.PrecioVentaEstimado);
    }

    // Regla de Negocio: Margen 0.01% = Venta al costo (Costo + Percepciones 50% y 40%)
    [Fact]
    public void PrecioVentaEstimado_MargenMinimo_RetornaCostoConPercepciones()
    {
        // Arrange: Costo 100. Percepciones: 100 * 1.5 * 1.4 = 210.
        var articulo = new Articulo { CostoVigente = 100m, MargenGanancia = 0.01m };

        // Act
        var precio = articulo.PrecioVentaEstimado;

        // Assert
        Assert.Equal(210m, precio);
    }

    // Regla de Negocio: Cálculo estándar
    [Fact]
    public void PrecioVentaEstimado_ConMargenNormal_RetornaCostoConPercepcionesYGanancia()
    {
        // Arrange: Costo 100. Base con percepciones: 210. Margen 10%. 210 * 1.10 = 231.
        var articulo = new Articulo { CostoVigente = 100m, MargenGanancia = 10m };

        // Act
        var precio = articulo.PrecioVentaEstimado;

        // Assert
        Assert.Equal(231m, precio);
    }
}
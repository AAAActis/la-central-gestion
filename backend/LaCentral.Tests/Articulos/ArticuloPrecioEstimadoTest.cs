using Xunit;
using LaCentral.UseCases.Entidades;
public class ArticuloPrecioEstimadoTests
{
    // CA-003 / TODO 1: Sin costo vigente
    [Fact]
    public void CalcularPrecioEstimado_SinCosto_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = null };
        articulo.Margenes.Add(new MargenModalidad(1, 40m));
        
        Assert.Null(articulo.CalcularPrecioEstimado(1));
    }

    // CA-003 / TODO 2: Sin márgenes
    [Fact]
    public void CalcularPrecioEstimado_SinMargen_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        // No agregamos márgenes
        
        Assert.Null(articulo.CalcularPrecioEstimado(1));
    }

    // Casos borde: 0.01% (Precio ≈ Costo)
    [Fact]
    public void CalcularPrecioEstimado_MargenMinimo_CalculaCorrectamente()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(1, 0.01m)); 

        // 100 * (1 + 0.0001) = 100.01
        var resultado = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(100.01m, resultado);
    }

    // Casos borde: 50% y 40% (Ex percepciones, ahora márgenes D1)
    [Theory]
    [InlineData(50, 150.00)]
    [InlineData(40, 140.00)]
    public void CalcularPrecioEstimado_MargenesEstandar_CalculaCorrectamente(decimal porcentaje, decimal esperado)
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(1, porcentaje));

        var resultado = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(esperado, resultado);
    }

    // Casos borde: Costo con centavos
    [Fact]
    public void CalcularPrecioEstimado_CostoConCentavos_AplicaMargenCorrectamente()
    {
        var articulo = new Articulo { CostoVigente = 125.50m };
        articulo.Margenes.Add(new MargenModalidad(1, 10m));

        // 125.50 * 1.10 = 138.05
        var resultado = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(138.05m, resultado);
    }

    // Casos borde: TODO 4 Redondeo explícito en ,xx5
    [Fact]
    public void CalcularPrecioEstimado_MilésimaCinco_RedondeaHaciaArriba()
    {
        var articulo = new Articulo { CostoVigente = 10.25m };
        articulo.Margenes.Add(new MargenModalidad(1, 10m));

        // 10.25 * 1.10 = 11.275 -> MidpointRounding.AwayFromZero debe dar 11.28
        var resultado = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(11.28m, resultado);
    }
}
using Xunit;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// Fórmula de Articulo.CalcularPrecioEstimado: costo × (1 + margen / 100), redondeada
/// a 2 decimales con la mitad hacia arriba. Costo desconocido es null, nunca cero (D1).
/// </summary>
public class ArticuloPrecioEstimadoTests
{
    // CA-003: sin costo vigente
    [Fact]
    public void CalcularPrecioEstimado_SinCosto_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = null };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 50m));

        Assert.Null(articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    // CA-003: sin márgenes cargados
    [Fact]
    public void CalcularPrecioEstimado_SinMargenes_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m };

        Assert.Null(articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    [Fact]
    public void CalcularPrecioEstimado_ModalidadSinMargenCargado_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 50m));

        Assert.Null(articulo.CalcularPrecioEstimado(Modalidad.Tarjeta3));
    }

    // D1: Costo = costo × 1,00 (0 %)
    [Fact]
    public void CalcularPrecioEstimado_ModalidadCostoAlCeroPorciento_RetornaMismoCosto()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Costo, 0m));

        Assert.Equal(100m, articulo.CalcularPrecioEstimado(Modalidad.Costo));
    }

    [Theory]
    [InlineData(10, 110.00)]
    [InlineData(40, 140.00)]
    [InlineData(50, 150.00)] // D1: Lista = costo × 1,50
    public void CalcularPrecioEstimado_Margenes_CalculaCostoMasPorcentaje(decimal porcentaje, decimal esperado)
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, porcentaje));

        Assert.Equal(esperado, articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    // Precisión de la fórmula con un margen fraccionario. No es una regla de modalidad.
    [Fact]
    public void CalcularPrecioEstimado_MargenFraccionario_ConservaCentavos()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 0.01m));

        // 100 × 1,0001 = 100,01
        Assert.Equal(100.01m, articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    [Fact]
    public void CalcularPrecioEstimado_CostoConCentavos_AplicaMargenCorrectamente()
    {
        var articulo = new Articulo { CostoVigente = 125.50m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 10m));

        // 125,50 × 1,10 = 138,05
        Assert.Equal(138.05m, articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    [Fact]
    public void CalcularPrecioEstimado_MilesimaCinco_RedondeaHaciaArriba()
    {
        var articulo = new Articulo { CostoVigente = 10.25m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 10m));

        // 10,25 × 1,10 = 11,275 → AwayFromZero da 11,28
        Assert.Equal(11.28m, articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }
}

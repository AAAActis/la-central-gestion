using Xunit;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Entidades;

public class ArticuloTests
{
    [Fact]
    public void ObtenerPrecioEstimado_SinCosto_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = null };
        articulo.Margenes.Add(new MargenModalidad(1, 20m));
        
        Assert.Null(articulo.CalcularPrecioEstimado(1));
    }

    [Fact]
    public void ObtenerPrecioEstimado_ModalidadInexistente_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        
        Assert.Null(articulo.CalcularPrecioEstimado(99));
    }

    // Regla D1: Costo neto al 0%
    [Fact]
    public void ObtenerPrecioEstimado_CostoAlCeroPorciento_RetornaMismoCosto()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(1, 0m)); // 0% de margen

        var precio = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(100m, precio);
    }

    [Fact]
    public void ObtenerPrecioEstimado_ConMargen_RetornaCostoMasPorcentaje()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(1, 10m)); // 10% de margen

        var precio = articulo.CalcularPrecioEstimado(1);

        Assert.Equal(110m, precio);
    }
}
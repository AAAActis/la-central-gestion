using Xunit;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Entidades;

public class ArticuloTests
{
    [Fact]
    public void ObtenerPrecioEstimado_SinCosto_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = null };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 20m));
        
        Assert.Null(articulo.CalcularPrecioEstimado(Modalidad.Lista));
    }

    [Fact]
    public void ObtenerPrecioEstimado_ModalidadSinMargenCargado_RetornaNull()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 50m));

        Assert.Null(articulo.CalcularPrecioEstimado(Modalidad.Tarjeta3));
    }

    // Regla D1: Costo neto al 0%
    [Fact]
    public void ObtenerPrecioEstimado_CostoAlCeroPorciento_RetornaMismoCosto()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Costo, 0m)); // 0% de margen

        var precio = articulo.CalcularPrecioEstimado(Modalidad.Costo);

        Assert.Equal(100m, precio);
    }

    [Fact]
    public void ObtenerPrecioEstimado_ConMargen_RetornaCostoMasPorcentaje()
    {
        var articulo = new Articulo { CostoVigente = 100m };
        articulo.Margenes.Add(new MargenModalidad(Modalidad.Lista, 10m)); // 10% de margen

        var precio = articulo.CalcularPrecioEstimado(Modalidad.Lista);

        Assert.Equal(110m, precio);
    }
}
using Xunit;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// Fórmula D1 de ReglasPrecioModalidad.Calcular: valor_de_la_base × (1 + porcentaje / 100),
/// redondeada a 2 decimales con la mitad hacia arriba. Costo desconocido es null, nunca cero.
/// Antes era Articulo.CalcularPrecioEstimado (siempre sobre el costo): los casos se conservan
/// reapuntados; los que dependían de "márgenes cargados" pasan a configuración sin fórmula.
/// </summary>
public class ArticuloPrecioEstimadoTests
{
    private static PrecioModalidad Lista(decimal porcentaje) => new()
    {
        Modalidad = Modalidad.Lista, Base = BasePrecio.CostoArticulo, Porcentaje = porcentaje
    };

    // CA-003: sin costo vigente
    [Fact]
    public void CalcularPrecioEstimado_SinCosto_RetornaNull()
    {
        Assert.Null(ReglasPrecioModalidad.Calcular(Lista(50m), costoArticulo: null, precioLista: null));
    }

    // Antes: "sin márgenes cargados". Hoy: Tarjeta3/6 sin tasa definida (base y porcentaje NULL).
    [Fact]
    public void CalcularPrecioEstimado_SinMargenes_RetornaNull()
    {
        var tarjeta = new PrecioModalidad { Modalidad = Modalidad.Tarjeta3 };

        Assert.Null(ReglasPrecioModalidad.Calcular(tarjeta, costoArticulo: 100m, precioLista: 150m));
    }

    // Antes: "modalidad sin margen cargado". Hoy: CasoEspecial, precio manual sin fórmula.
    [Fact]
    public void CalcularPrecioEstimado_ModalidadSinMargenCargado_RetornaNull()
    {
        var especial = new PrecioModalidad { Modalidad = Modalidad.CasoEspecial, CalculoManual = true };

        Assert.Null(ReglasPrecioModalidad.Calcular(especial, costoArticulo: 100m, precioLista: 150m));
    }

    // Regla D1: Costo neto al 0 % sobre el costo del artículo
    [Fact]
    public void CalcularPrecioEstimado_ModalidadCostoAlCeroPorciento_RetornaMismoCosto()
    {
        var costo = new PrecioModalidad { Modalidad = Modalidad.Costo, Base = BasePrecio.CostoArticulo, Porcentaje = 0m };

        Assert.Equal(100m, ReglasPrecioModalidad.Calcular(costo, costoArticulo: 100m, precioLista: null));
    }

    [Theory]
    [InlineData(10, 110.00)]
    [InlineData(40, 140.00)]
    [InlineData(50, 150.00)] // D1: Lista = costo × 1,50
    public void CalcularPrecioEstimado_Margenes_CalculaCostoMasPorcentaje(decimal porcentaje, decimal esperado)
    {
        Assert.Equal(esperado, ReglasPrecioModalidad.Calcular(Lista(porcentaje), costoArticulo: 100m, precioLista: null));
    }

    // Precisión de la fórmula con un margen fraccionario. No es una regla de modalidad.
    [Fact]
    public void CalcularPrecioEstimado_MargenFraccionario_ConservaCentavos()
    {
        // 100 × 1,0001 = 100,01
        Assert.Equal(100.01m, ReglasPrecioModalidad.Calcular(Lista(0.01m), costoArticulo: 100m, precioLista: null));
    }

    [Fact]
    public void CalcularPrecioEstimado_CostoConCentavos_AplicaMargenCorrectamente()
    {
        // 125,50 × 1,10 = 138,05
        Assert.Equal(138.05m, ReglasPrecioModalidad.Calcular(Lista(10m), costoArticulo: 125.50m, precioLista: null));
    }

    [Fact]
    public void CalcularPrecioEstimado_MilesimaCinco_RedondeaHaciaArriba()
    {
        // 10,25 × 1,10 = 11,275 → AwayFromZero da 11,28
        Assert.Equal(11.28m, ReglasPrecioModalidad.Calcular(Lista(10m), costoArticulo: 10.25m, precioLista: null));
    }

    // ---- Base de cálculo (script 04) ----

    // CostoArticulo es el costo del artículo: el precio de Lista que se pase no interviene.
    [Fact]
    public void Calcular_BaseCostoArticulo_IgnoraElPrecioDeLista()
    {
        Assert.Equal(150m, ReglasPrecioModalidad.Calcular(Lista(50m), costoArticulo: 100m, precioLista: 999m));
    }

    // D1: Efectivo = Lista × 0,90, un porcentaje negativo sobre PrecioLista.
    [Fact]
    public void Calcular_EfectivoSobrePrecioLista_AplicaElDescuento()
    {
        var efectivo = new PrecioModalidad
        {
            Modalidad = Modalidad.EfectivoTransferencia, Base = BasePrecio.PrecioLista, Porcentaje = -10m
        };

        Assert.Equal(135m, ReglasPrecioModalidad.Calcular(efectivo, costoArticulo: 999m, precioLista: 150m));
    }

    // Sin precio de Lista no hay Efectivo, aunque haya costo: no se sustituye una base por otra.
    [Fact]
    public void Calcular_BasePrecioListaSinPrecioDeLista_RetornaNull()
    {
        var efectivo = new PrecioModalidad
        {
            Modalidad = Modalidad.EfectivoTransferencia, Base = BasePrecio.PrecioLista, Porcentaje = -10m
        };

        Assert.Null(ReglasPrecioModalidad.Calcular(efectivo, costoArticulo: 100m, precioLista: null));
    }
}

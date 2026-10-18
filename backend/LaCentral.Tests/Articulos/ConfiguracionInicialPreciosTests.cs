using Xunit;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// Decisión C: el alta crea las SEIS filas de configuración. Así la consulta distingue las
/// tres razones de ausencia: sin fila = sin configuración; precio null con costo null = sin
/// costo; precio null con calculo_manual = caso especial no fijado.
/// </summary>
public class ConfiguracionInicialPreciosTests
{
    [Fact]
    public void ConfiguracionInicial_SeisFilasConBaseYPorcentajeDeD1YPrecioNulo()
    {
        Assert.Equal(new PrecioModalidad[]
        {
            new() { Modalidad = Modalidad.Lista, Base = BasePrecio.CostoArticulo, Porcentaje = 50m },
            new() { Modalidad = Modalidad.Tarjeta3 },
            new() { Modalidad = Modalidad.Tarjeta6 },
            new() { Modalidad = Modalidad.EfectivoTransferencia, Base = BasePrecio.PrecioLista, Porcentaje = -10m },
            new() { Modalidad = Modalidad.Costo, Base = BasePrecio.CostoArticulo, Porcentaje = 0m },
            new() { Modalidad = Modalidad.CasoEspecial, CalculoManual = true },
        }, ReglasPrecioModalidad.ConfiguracionInicial());
    }

    // Cada alta recibe sus propias filas: cambiar una no puede tocar la del próximo artículo.
    [Fact]
    public void ConfiguracionInicial_DevuelveFilasNuevasEnCadaLlamada()
    {
        var primera = ReglasPrecioModalidad.ConfiguracionInicial();
        Assert.Equal(6, primera.Count);
        primera[0].Precio = 1m;

        var segunda = ReglasPrecioModalidad.ConfiguracionInicial();

        Assert.Equal(6, segunda.Count);
        Assert.All(segunda, p => Assert.Null(p.Precio));
    }

    // La configuración inicial y la fórmula, juntas: con costo 100, Lista 150 y Efectivo 135.
    [Fact]
    public void ConfiguracionInicial_ConCosto_DaLosPreciosDeD1()
    {
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial();
        Assert.Equal(6, configuracion.Count);
        var lista = CalcularPreciosArticuloUseCase.Calcular(configuracion.Single(p => p.Modalidad == Modalidad.Lista), 100m, null);

        var precios = configuracion.ToDictionary(p => p.Modalidad, p => CalcularPreciosArticuloUseCase.Calcular(p, 100m, lista));

        Assert.Equal(150m, precios[Modalidad.Lista]);
        Assert.Equal(135m, precios[Modalidad.EfectivoTransferencia]);
        Assert.Equal(100m, precios[Modalidad.Costo]);
        Assert.Null(precios[Modalidad.Tarjeta3]);
        Assert.Null(precios[Modalidad.Tarjeta6]);
        Assert.Null(precios[Modalidad.CasoEspecial]);
    }
}

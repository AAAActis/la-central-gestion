using Xunit;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-05 · CalcularPreciosArticuloUseCase.Recalcular: las filas de un artículo con el
/// precio de cada modalidad automática recalculado desde el costo. Las manuales no se tocan.
/// </summary>
public class RecalcularPreciosTests
{
    private static readonly DateTime Ahora = new(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime CalculoAnterior = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    // Tasas SOLO DE PRUEBA para ejercitar la fórmula en Tarjeta3/6. NO son tasas comerciales:
    // D1 las deja sin definir y en producción esas filas no tienen base ni porcentaje.
    private const decimal TasaTarjeta3DePrueba = 7m;
    private const decimal TasaTarjeta6DePrueba = 13m;

    private static PrecioModalidad Fila(IReadOnlyList<PrecioModalidad> filas, Modalidad modalidad) =>
        filas.Single(p => p.Modalidad == modalidad);

    // Ejemplos obligatorios del plan, con costo 100.
    [Fact]
    public void ConfiguracionInicialYCosto100_Lista150Costo100Efectivo135()
    {
        var precios = CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), 100m, Ahora);

        Assert.Equal(150m, Fila(precios, Modalidad.Lista).Precio);
        Assert.Equal(100m, Fila(precios, Modalidad.Costo).Precio);
        Assert.Equal(135m, Fila(precios, Modalidad.EfectivoTransferencia).Precio);
        Assert.Null(Fila(precios, Modalidad.Tarjeta3).Precio);
        Assert.Null(Fila(precios, Modalidad.Tarjeta6).Precio);
        Assert.Null(Fila(precios, Modalidad.CasoEspecial).Precio);
        Assert.Equal(6, precios.Count);
    }

    // Una fila con precio lleva la fecha del cálculo y ningún usuario (lo calculó el sistema);
    // una sin precio no lleva fecha (CHECK precio_con_fecha).
    [Fact]
    public void FechaSoloEnLasQueTienenPrecio_YSinUsuarioEnLasCalculadas()
    {
        var precios = CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), 100m, Ahora);

        foreach (var modalidad in new[] { Modalidad.Lista, Modalidad.Costo, Modalidad.EfectivoTransferencia })
            Assert.Equal((Ahora, (int?)null), (Fila(precios, modalidad).FechaCalculo, Fila(precios, modalidad).UsuarioId));
        Assert.Null(Fila(precios, Modalidad.Tarjeta3).FechaCalculo);
        Assert.Null(Fila(precios, Modalidad.Tarjeta6).FechaCalculo);
    }

    // Efectivo sale de la Lista que se acaba de calcular, no de la que estaba guardada.
    [Fact]
    public void EfectivoSaleDeLaListaRecienCalculada_NoDeLaGuardada()
    {
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial()
            .Select(p => p.Modalidad == Modalidad.Lista ? p with { Precio = 999m, FechaCalculo = CalculoAnterior } : p)
            .ToList();

        var precios = CalcularPreciosArticuloUseCase.Recalcular(configuracion, 100m, Ahora);

        Assert.Equal(150m, Fila(precios, Modalidad.Lista).Precio);
        Assert.Equal(135m, Fila(precios, Modalidad.EfectivoTransferencia).Precio);
    }

    // Costo desconocido: null, nunca cero, y sin fecha. Los precios viejos no sobreviven.
    [Fact]
    public void SinCosto_LasAutomaticasQuedanNullYSinFecha()
    {
        var anteriores = new Dictionary<Modalidad, decimal>
        {
            [Modalidad.Lista] = 150m, [Modalidad.Costo] = 100m, [Modalidad.EfectivoTransferencia] = 135m
        };
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial()
            .Select(p => anteriores.TryGetValue(p.Modalidad, out var precio)
                ? p with { Precio = precio, FechaCalculo = CalculoAnterior }
                : p)
            .ToList();

        var precios = CalcularPreciosArticuloUseCase.Recalcular(configuracion, null, Ahora);

        foreach (var modalidad in new[] { Modalidad.Lista, Modalidad.Costo, Modalidad.EfectivoTransferencia })
            Assert.Equal(((decimal?)null, (DateTime?)null), (Fila(precios, modalidad).Precio, Fila(precios, modalidad).FechaCalculo));
    }

    // La política vigente: el recálculo no pisa una fila manual (CasoEspecial).
    [Fact]
    public void FilaManual_SaleIntacta()
    {
        var especial = new PrecioModalidad
        {
            Modalidad = Modalidad.CasoEspecial, CalculoManual = true, Precio = 80m, FechaCalculo = CalculoAnterior, UsuarioId = 7
        };
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial()
            .Select(p => p.Modalidad == Modalidad.CasoEspecial ? especial : p).ToList();

        var precios = CalcularPreciosArticuloUseCase.Recalcular(configuracion, 100m, Ahora);

        Assert.Equal(especial, Fila(precios, Modalidad.CasoEspecial));
    }

    // D11: con Lista manual, Efectivo se calcula sobre ese precio de lista (× 0,90), y la
    // Lista manual no se toca.
    [Fact]
    public void ListaManual_EfectivoSobreLaListaManual_YLaListaNoSeToca()
    {
        var listaManual = new PrecioModalidad
        {
            Modalidad = Modalidad.Lista, CalculoManual = true, Precio = 200m, FechaCalculo = CalculoAnterior, UsuarioId = 7
        };
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial()
            .Select(p => p.Modalidad == Modalidad.Lista ? listaManual : p).ToList();

        var precios = CalcularPreciosArticuloUseCase.Recalcular(configuracion, 100m, Ahora);

        Assert.Equal(listaManual, Fila(precios, Modalidad.Lista));
        Assert.Equal(180m, Fila(precios, Modalidad.EfectivoTransferencia).Precio);
        Assert.Equal(100m, Fila(precios, Modalidad.Costo).Precio);
    }

    // Tarjeta3/6 con tasas de prueba: cuando el equipo cargue las reales (como configuración),
    // la misma fórmula las aplica. Sin tasa, null.
    [Fact]
    public void TarjetasConTasasDePrueba_AplicanLaFormula()
    {
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial().Select(p => p.Modalidad switch
        {
            Modalidad.Tarjeta3 => p with { Base = BasePrecio.PrecioLista, Porcentaje = TasaTarjeta3DePrueba },
            Modalidad.Tarjeta6 => p with { Base = BasePrecio.PrecioLista, Porcentaje = TasaTarjeta6DePrueba },
            _ => p
        }).ToList();

        var precios = CalcularPreciosArticuloUseCase.Recalcular(configuracion, 100m, Ahora);

        Assert.Equal(160.50m, Fila(precios, Modalidad.Tarjeta3).Precio);   // 150 × 1,07
        Assert.Equal(169.50m, Fila(precios, Modalidad.Tarjeta6).Precio);   // 150 × 1,13
    }

    // Filas nuevas: la configuración que entra no cambia.
    [Fact]
    public void NoModificaLasFilasDeEntrada()
    {
        var configuracion = ReglasPrecioModalidad.ConfiguracionInicial();

        CalcularPreciosArticuloUseCase.Recalcular(configuracion, 100m, Ahora);

        Assert.All(configuracion, p => Assert.Null(p.Precio));
    }
}

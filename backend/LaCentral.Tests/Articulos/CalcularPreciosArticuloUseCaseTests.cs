using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-05 · CalcularPreciosArticuloUseCase.EjecutarAsync con el puerto doble: qué lee,
/// qué escribe y cuándo rechaza. Que la fila se escriba (y se revierta) se prueba contra
/// PostgreSQL en RegistrarFacturaCompraPostgreSqlTests.
/// </summary>
public class CalcularPreciosArticuloUseCaseTests
{
    private static readonly DateTime Ahora = new(2026, 10, 16, 12, 30, 0, DateTimeKind.Utc);

    // articulo_precio_modalidad.precio es NUMERIC(12,2): el máximo es 9.999.999.999,99.
    // Lista = costo × 1,50, así que estos dos costos son los dos lados del borde.
    private const decimal CostoConListaEnElMaximo = 6_666_666_666.66m;   // Lista 9.999.999.999,99
    private const decimal CostoConListaQueNoEntra = 6_666_666_666.67m;   // Lista 10.000.000.000,01

    private readonly Mock<IPreciosArticuloRepositorio> _precios = new();
    private readonly Dictionary<int, IReadOnlyList<PrecioModalidad>> _escritos = new();

    public CalcularPreciosArticuloUseCaseTests()
    {
        _precios.Setup(p => p.ActualizarPreciosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<PrecioModalidad>>(), It.IsAny<CancellationToken>()))
            .Callback((int id, IReadOnlyList<PrecioModalidad> filas, CancellationToken _) => _escritos[id] = filas)
            .Returns(Task.CompletedTask);
    }

    private CalcularPreciosArticuloUseCase Caso() => new(_precios.Object);

    private void Existentes(params (int Id, decimal? Costo, IReadOnlyList<PrecioModalidad> Filas)[] articulos) =>
        _precios.Setup(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articulos.ToDictionary(a => a.Id, a => new PreciosDeArticulo(a.Costo, a.Filas)));

    private IReadOnlyList<PrecioModalidad> Escritas(int articuloId)
    {
        Assert.Contains(articuloId, _escritos.Keys);
        return _escritos[articuloId];
    }

    private static decimal? Precio(IReadOnlyList<PrecioModalidad> filas, Modalidad modalidad) =>
        filas.Single(p => p.Modalidad == modalidad).Precio;

    [Fact]
    public async Task EscribeLosPreciosRecalculadosDeCadaArticulo()
    {
        Existentes((1, 100m, ReglasPrecioModalidad.ConfiguracionInicial()), (2, null, ReglasPrecioModalidad.ConfiguracionInicial()));

        var resultado = await Caso().EjecutarAsync(new[] { 1, 2 }, Ahora);

        Assert.True(resultado.IsSuccess, resultado.Error);
        var primero = Escritas(1);
        Assert.Equal((150m, 100m, 135m),
            (Precio(primero, Modalidad.Lista), Precio(primero, Modalidad.Costo), Precio(primero, Modalidad.EfectivoTransferencia)));
        Assert.Equal(CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), 100m, Ahora), primero);
        Assert.All(Escritas(2), p => Assert.Null(p.Precio));
    }

    [Fact]
    public async Task PideSoloLosArticulosIndicados()
    {
        Existentes();

        await Caso().EjecutarAsync(new[] { 1, 2 }, Ahora);

        _precios.Verify(p => p.ObtenerAsync(
            It.Is<IReadOnlyCollection<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 1, 2 })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Un artículo sin filas de precio (anterior a D1) no tiene nada que recalcular: la
    // ficha lo informa como SinConfiguracion.
    [Fact]
    public async Task ArticuloSinFilasDePrecio_NoSeEscribe()
    {
        Existentes((3, 100m, Array.Empty<PrecioModalidad>()));

        var resultado = await Caso().EjecutarAsync(new[] { 3 }, Ahora);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Empty(_escritos);
    }

    // Un precio que la columna no guarda daría 22003 → 500. Es Invalido, y no se escribe el
    // de ningún artículo: quien llama descarta la operación entera.
    [Fact]
    public async Task PrecioQueNoEntraEnSuColumna_InvalidoYNoEscribeNinguno()
    {
        Existentes((1, 100m, ReglasPrecioModalidad.ConfiguracionInicial()),
            (2, CostoConListaQueNoEntra, ReglasPrecioModalidad.ConfiguracionInicial()));

        var resultado = await Caso().EjecutarAsync(new[] { 1, 2 }, Ahora);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("artículo 2", resultado.Error);
        Assert.Contains("Lista", resultado.Error);
        Assert.Empty(_escritos);
    }

    [Fact]
    public async Task PrecioEnElMaximoDeSuColumna_SeEscribe()
    {
        Existentes((1, CostoConListaEnElMaximo, ReglasPrecioModalidad.ConfiguracionInicial()));

        var resultado = await Caso().EjecutarAsync(new[] { 1 }, Ahora);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(9_999_999_999.99m, Precio(Escritas(1), Modalidad.Lista));
    }
}

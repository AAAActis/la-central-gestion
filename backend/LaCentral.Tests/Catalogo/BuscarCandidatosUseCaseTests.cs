using LaCentral.UseCases.Catalogo;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Catalogo;

/// <summary>053 · La lógica del caso de uso, con el repositorio simulado. Lo que llega a la base está en CandidatosApiPostgreSqlTests.</summary>
public class BuscarCandidatosUseCaseTests
{
    private static CandidatoCatalogoDto Fila(int id, double? parecido = null)
        => new(id, 1, "Proveedor", $"C-{id}", $"Fila {id}", null, null, parecido, null);

    [Fact]
    public async Task SinTextoNiArticulo_EsInvalido_YNoConsultaElCatalogo()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>(MockBehavior.Strict);
        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new()).EjecutarAsync("   ", null);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }

    [Fact]
    public async Task ConArticuloInexistente_EsNoEncontrado()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>();
        catalogo.Setup(c => c.TextosDeArticuloAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((IReadOnlyList<string>?)null);
        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new()).EjecutarAsync(null, 7);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task CA2_LaClaveEsSinSeparadoresNiMayusculasNiAcentos_YUnaExactaNoSeRepiteEntreLasAproximadas()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>();
        catalogo.Setup(c => c.BuscarExactasAsync("GN100", It.IsAny<CancellationToken>())).ReturnsAsync([Fila(1)]);
        catalogo.Setup(c => c.BuscarAproximadasAsync(new[] { "gn 100" }, 0.3, 51, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Fila(1, 0.9), Fila(2, 0.5)]);

        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new()).EjecutarAsync(" gn 100 ", null);

        Assert.True(resultado.IsSuccess);
        Assert.Equal([1], resultado.Value.Exactas.Select(e => e.Id));
        Assert.Equal([2], resultado.Value.Aproximadas.Select(a => a.Id));
        Assert.Null(resultado.Value.Mensaje);
    }

    [Fact]
    public async Task NormalizacionCatalogo_Clave()
    {
        Assert.Equal("GN100", NormalizacionCatalogo.Clave("GN/100"));
        Assert.Equal("GN100", NormalizacionCatalogo.Clave("gn 100"));
        Assert.Equal("BOMBAAGUANANDU", NormalizacionCatalogo.Clave("Bomba-agua ñandú"));
        await Task.CompletedTask;
    }

    [Fact]
    public async Task CA5_ConArticulo_BuscaTambienPorSuNombreYElDeSusFilasVinculadas_SinRepetirTextos()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>();
        catalogo.Setup(c => c.TextosDeArticuloAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(["Bomba de agua", "BOMBA AGUA FIAT 147", "bomba de agua"]);
        catalogo.Setup(c => c.BuscarAproximadasAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new()).EjecutarAsync(null, 7);

        catalogo.Verify(c => c.BuscarAproximadasAsync(
            It.Is<IReadOnlyList<string>>(t => t.SequenceEqual(new[] { "Bomba de agua", "BOMBA AGUA FIAT 147" })),
            0.3, 50, It.IsAny<CancellationToken>()));
        // Sin texto no hay clave: no se buscan exactas.
        catalogo.Verify(c => c.BuscarExactasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("Bomba de agua", resultado.Value.Texto);
    }

    [Fact]
    public async Task CA6_SinExactasNiAproximadas_InformaQueNoHayCandidatosParaElTexto()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>();
        catalogo.Setup(c => c.BuscarExactasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        catalogo.Setup(c => c.BuscarAproximadasAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<double>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new()).EjecutarAsync("xyz 999", null);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("No hay candidatos para «xyz 999».", resultado.Value.Mensaje);
    }

    [Fact]
    public async Task ElUmbralYElMaximoSonLosDeLasOpciones()
    {
        var catalogo = new Mock<ICatalogoCandidatosRepositorio>();
        catalogo.Setup(c => c.BuscarExactasAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        catalogo.Setup(c => c.BuscarAproximadasAsync(It.IsAny<IReadOnlyList<string>>(), 0.6, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync([Fila(1, 0.9), Fila(2, 0.8), Fila(3, 0.7)]);

        var resultado = await new BuscarCandidatosUseCase(catalogo.Object, new(0.6, 2)).EjecutarAsync("bomba", null);

        Assert.Equal([1, 2], resultado.Value.Aproximadas.Select(a => a.Id));
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(1.01, 50)]
    [InlineData(0.3, 0)]
    public void ConOpcionesFueraDeRango_NoSeConstruye(double umbral, int maximo)
        => Assert.Throws<ArgumentOutOfRangeException>(() =>
            new BuscarCandidatosUseCase(Mock.Of<ICatalogoCandidatosRepositorio>(), new(umbral, maximo)));
}

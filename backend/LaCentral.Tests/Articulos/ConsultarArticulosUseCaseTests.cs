using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>HU-ART-02 · la validación de la búsqueda es del caso de uso, con Result.</summary>
public class ConsultarArticulosUseCaseTests
{
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();

    private ConsultarArticulosUseCase Caso() => new(_consulta.Object);

    private static PaginaArticulosDto Pagina(params ArticuloResultadoBusquedaDto[] articulos) =>
        new(articulos, articulos.Length, 1, 20, Array.Empty<ConflictoCodigoArticuloDto>());

    // Movida desde ArticuloRepositorioTests.Consulta_RechazaEntradaOPaginaInvalida, que
    // esperaba ArgumentException del repositorio.
    [Theory]
    [InlineData("", 1, 20)]
    [InlineData("   ", 1, 20)]
    [InlineData("* *", 1, 20)]
    [InlineData("mouse*\t", 1, 20)]
    [InlineData("mouse", 0, 20)]
    [InlineData("mouse", 1, 0)]
    [InlineData("mouse", 1, 101)]
    [InlineData("mouse", int.MaxValue, 100)]
    public async Task Consulta_EntradaOPaginaInvalida_InvalidoSinConsultarElRepositorio(string texto, int pagina, int tamano)
    {
        var resultado = await Caso().EjecutarAsync(new ConsultaArticulos(texto, Pagina: pagina, TamanoPagina: tamano));

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
        _consulta.Verify(c => c.BuscarAsync(It.IsAny<ConsultaArticulos>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consulta_TextoDeMasDe500_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new ConsultaArticulos(new string('a', 501)));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _consulta.Verify(c => c.BuscarAsync(It.IsAny<ConsultaArticulos>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consulta_TextoDe500_ConsultaElRepositorio()
    {
        _consulta.Setup(c => c.BuscarAsync(It.IsAny<ConsultaArticulos>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Pagina());

        var resultado = await Caso().EjecutarAsync(new ConsultaArticulos(new string('a', 500)));

        Assert.True(resultado.IsSuccess);
    }

    [Fact]
    public async Task Consulta_Valida_PasaLaConsultaTalCualYDevuelveLaPagina()
    {
        var consulta = new ConsultaArticulos("bujia", IncluirInactivos: true, Pagina: 3, TamanoPagina: 7);
        var pagina = Pagina(new ArticuloResultadoBusquedaDto(42, "LC-000042", "Bujía", true,
            new[] { new CodigoProveedorResumenDto(5, "Proveedor", "BPR6ES") }));
        _consulta.Setup(c => c.BuscarAsync(consulta, It.IsAny<CancellationToken>())).ReturnsAsync(pagina);

        var resultado = await Caso().EjecutarAsync(consulta);

        Assert.True(resultado.IsSuccess);
        Assert.Same(pagina, resultado.Value);
        _consulta.Verify(c => c.BuscarAsync(consulta, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Sin resultados NO es NoEncontrado: es una página vacía (200, nunca 404).
    [Fact]
    public async Task Consulta_SinResultados_ExitoConListaVacia()
    {
        _consulta.Setup(c => c.BuscarAsync(It.IsAny<ConsultaArticulos>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Pagina());

        var resultado = await Caso().EjecutarAsync(new ConsultaArticulos("inexistente"));

        Assert.True(resultado.IsSuccess);
        Assert.Empty(resultado.Value!.Articulos);
    }
}

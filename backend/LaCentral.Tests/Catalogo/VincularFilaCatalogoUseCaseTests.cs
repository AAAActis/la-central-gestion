using LaCentral.UseCases.Catalogo;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Catalogo;

/// <summary>054a · La lógica del caso de uso, con el repositorio simulado. Que la fila se escriba se prueba contra PostgreSQL.</summary>
public class VincularFilaCatalogoUseCaseTests
{
    private readonly Mock<ICatalogoVinculacionRepositorio> _catalogo = new();
    private readonly Mock<IContextoUsuario> _contexto = new();

    public VincularFilaCatalogoUseCaseTests()
    {
        _contexto.SetupGet(c => c.UsuarioId).Returns(42);
        _catalogo.Setup(c => c.ObtenerFilaAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FilaParaVincularDto(10, 3, " gn-100 ", false, true));
        _catalogo.Setup(c => c.ArticuloActivoAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogo.Setup(c => c.VincularAsync(5, 3, "GN-100", 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private Task<Result<VinculacionDto>> Vincular(int filaId = 10, int articuloId = 5)
        => new VincularFilaCatalogoUseCase(_catalogo.Object, _contexto.Object).EjecutarAsync(filaId, articuloId);

    private void NoVincula()
        => _catalogo.Verify(c => c.VincularAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task CA1_VinculaConElUsuarioDeLaSesionYElCodigoNormalizadoComoLaFicha()
    {
        var resultado = await Vincular();

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new VinculacionDto(5, 3, "GN-100"), resultado.Value);
        _catalogo.Verify(c => c.VincularAsync(5, 3, "GN-100", 42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FilaInexistente_EsNoEncontrado_YNoVincula()
    {
        var resultado = await Vincular(filaId: 99);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoVincula();
    }

    [Theory]
    [InlineData(true, true)]   // D-S7-10: vencida
    [InlineData(false, false)] // CA9: proveedor de baja
    public async Task FilaVencidaODeProveedorDeBaja_EsInvalido_YNoVincula(bool vencido, bool proveedorActivo)
    {
        _catalogo.Setup(c => c.ObtenerFilaAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FilaParaVincularDto(10, 3, "GN-100", vencido, proveedorActivo));
        var resultado = await Vincular();
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoVincula();
    }

    [Fact]
    public async Task ArticuloInexistente_EsNoEncontrado_YNoVincula()
    {
        var resultado = await Vincular(articuloId: 77);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoVincula();
    }

    [Fact]
    public async Task ArticuloDeBaja_EsInvalido_YNoVincula()
    {
        _catalogo.Setup(c => c.ArticuloActivoAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var resultado = await Vincular();
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoVincula();
    }

    [Fact]
    public async Task CA6_CodigoYaVinculadoAOtroArticulo_EsConflicto_DiceCualEs_YNoVincula()
    {
        _catalogo.Setup(c => c.VinculadoAAsync(3, "GN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloVinculadoDto(8, "BOM-008", "Bomba de agua"));
        var resultado = await Vincular();
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("BOM-008", resultado.Error);
        Assert.Contains("Bomba de agua", resultado.Error);
        NoVincula();
    }

    [Fact]
    public async Task YaVinculadoAlMismoArticulo_EsExito_YNoVuelveAEscribir()
    {
        _catalogo.Setup(c => c.VinculadoAAsync(3, "GN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloVinculadoDto(5, "ART-005", "Bomba"));
        var resultado = await Vincular();
        Assert.True(resultado.IsSuccess, resultado.Error);
        NoVincula();
    }

    [Fact]
    public async Task CA6_OtroLoVinculoEntreLaConsultaYLaEscritura_EsConflicto_YDiceCualEs()
    {
        _catalogo.Setup(c => c.VincularAsync(5, 3, "GN-100", 42, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _catalogo.SetupSequence(c => c.VinculadoAAsync(3, "GN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ArticuloVinculadoDto?)null)
            .ReturnsAsync(new ArticuloVinculadoDto(8, "BOM-008", "Bomba de agua"));
        var resultado = await Vincular();
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("BOM-008", resultado.Error);
    }
}

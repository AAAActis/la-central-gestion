using LaCentral.UseCases.Comparador;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Comparador;

/// <summary>030, 032 y 054 CA4 · La lógica del comparador, con el repositorio simulado. La base, en ComparadorApiPostgreSqlTests.</summary>
public class ObtenerComparadorUseCaseTests
{
    private static readonly DateTime Lectura = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    private static FilaCatalogoVinculada Fila(int proveedor, string codigo, decimal? neto, decimal? lista = null,
        string? estado = null, decimal? stock = null)
        => new(proveedor, $"Proveedor {proveedor}", null, codigo, $"Fila {codigo}", null, neto, lista, estado, stock, null, Lectura);

    private static async Task<ComparadorDto> CompararAsync(int? ultimoProveedor, params FilaCatalogoVinculada[] filas)
    {
        var repositorio = new Mock<IComparadorRepositorio>();
        repositorio.Setup(r => r.ObtenerAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new DatosComparador(ultimoProveedor, filas));
        var resultado = await new ObtenerComparadorUseCase(repositorio.Object).EjecutarAsync(5);
        Assert.True(resultado.IsSuccess);
        return resultado.Value;
    }

    [Fact]
    public async Task CA002_030_OrdenaPorPrecioNetoAscendente_YLaFilaSinNetoVaAlFinal()
    {
        var c = await CompararAsync(null, Fila(1, "A", 300m), Fila(2, "B", null, 50m), Fila(3, "C", 100m), Fila(4, "D", 200m));
        Assert.Equal(["C", "D", "A", "B"], c.Filas.Select(f => f.Codigo));
    }

    [Fact]
    public async Task D_S7_09_SeComparaElNeto_NoElDeLista()
    {
        // La de lista más baja no es la más barata.
        var c = await CompararAsync(null, Fila(1, "A", 120m, lista: 150m), Fila(2, "B", 100m, lista: 400m));
        Assert.Equal(["B", "A"], c.Filas.Select(f => f.Codigo));
    }

    [Fact]
    public async Task CA002_032_LaDiferenciaEsContraElMasBarato_YSinNetoNoHayDiferencia()
    {
        var c = await CompararAsync(null, Fila(1, "A", 150.5m), Fila(2, "B", 100m), Fila(3, "C", null, 90m));
        Assert.Equal([(decimal?)0m, 50.5m, null], c.Filas.Select(f => f.DiferenciaConMasBarato));
    }

    [Fact]
    public async Task CA003_032_SenalaAlUltimoProveedorComprado()
    {
        var c = await CompararAsync(2, Fila(1, "A", 100m), Fila(2, "B", 200m));
        Assert.Equal([false, true], c.Filas.Select(f => f.UltimoComprado));
    }

    [Fact]
    public async Task CA003_030_ConUnSoloProveedor_LoMuestraIgual()
    {
        var c = await CompararAsync(null, Fila(1, "A", 100m));
        Assert.Equal([(decimal?)0m], c.Filas.Select(f => f.DiferenciaConMasBarato));
        Assert.Null(c.Mensaje);
    }

    [Fact]
    public async Task CA4_054_LaExistenciaDeUnProveedorEsElMaximoEntreSusFilas_NuncaLaSuma()
    {
        var c = await CompararAsync(null,
            Fila(1, "A", 100m, estado: "NO_HAY", stock: 2m), Fila(1, "A2", 110m, estado: "HAY", stock: 5m),
            Fila(1, "A3", 120m, estado: "A_CONFIRMAR", stock: null), Fila(2, "B", 90m, estado: "A_CONFIRMAR", stock: 1m));

        Assert.Equal([new ExistenciaProveedorDto(1, "HAY", 5m), new ExistenciaProveedorDto(2, "A_CONFIRMAR", 1m)], c.ExistenciaPorProveedor);
        Assert.Equal(4, c.Filas.Count);   // D-S7-06: cada código es su propia fila.
    }

    [Fact]
    public async Task CA004_030_SinFilasVinculadas_LoInforma()
    {
        var c = await CompararAsync(null);
        Assert.Empty(c.Filas);
        Assert.Equal(ObtenerComparadorUseCase.SinProveedores, c.Mensaje);
    }

    [Fact]
    public async Task ArticuloInexistente_EsNoEncontrado()
    {
        var repositorio = new Mock<IComparadorRepositorio>();
        repositorio.Setup(r => r.ObtenerAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((DatosComparador?)null);
        var resultado = await new ObtenerComparadorUseCase(repositorio.Object).EjecutarAsync(9);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }
}

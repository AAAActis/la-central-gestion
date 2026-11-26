using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Catalogo;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Catalogo;

/// <summary>054a CA5 · La composición, con los repositorios simulados. Que el fallo no deje el artículo se prueba contra PostgreSQL.</summary>
public class CrearYVincularArticuloUseCaseTests
{
    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<ICatalogoVinculacionRepositorio> _catalogo = new();
    private readonly Mock<IUnidadDeTrabajo> _unidad = new();
    private readonly Mock<IContextoUsuario> _contexto = new();
    private int _transacciones;

    public CrearYVincularArticuloUseCaseTests()
    {
        _contexto.SetupGet(c => c.UsuarioId).Returns(42);
        _unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result>> operacion, CancellationToken ct) => { _transacciones++; return operacion(ct); });
        _articulos.Setup(a => a.ObtenerSucursalesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new short[] { 1 });
        _articulos.Setup(a => a.AgregarAsync(It.IsAny<Articulo>(), It.IsAny<CancellationToken>())).ReturnsAsync(31);
        _articulos.Setup(a => a.AsignarCodigoInternoAsync(31, "LC-000031", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogo.Setup(c => c.ObtenerFilaAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FilaParaVincularDto(10, 3, "gn-100", false, true));
        _catalogo.Setup(c => c.ArticuloActivoAsync(31, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogo.Setup(c => c.VincularAsync(31, 3, "GN-100", 42, It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    private Task<Result<AltaVinculadaDto>> Ejecutar(string nombre = "Bomba de agua")
        => new CrearYVincularArticuloUseCase(
                new CrearArticuloUseCase(_articulos.Object, Mock.Of<IConsultaArticulosRepositorio>(), Mock.Of<IProveedorRepositorio>(), _unidad.Object),
                new VincularFilaCatalogoUseCase(_catalogo.Object, _contexto.Object),
                _unidad.Object)
            .EjecutarAsync(10, nombre);

    [Fact]
    public async Task CA5_CreaElArticuloYLoVincula_EnUnaSolaUnidadDeTrabajoExterior()
    {
        var resultado = await Ejecutar();

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new AltaVinculadaDto(31, "LC-000031", 3, "GN-100"), resultado.Value);
        _articulos.Verify(a => a.AgregarAsync(It.Is<Articulo>(x => x.Nombre == "Bomba de agua"), It.IsAny<CancellationToken>()), Times.Once);
        _catalogo.Verify(c => c.VincularAsync(31, 3, "GN-100", 42, It.IsAny<CancellationToken>()), Times.Once);
        // La exterior y la anidada del alta: la anidada la confirma o descarta la exterior.
        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CA5_SiElAltaFalla_NoVincula_YDevuelveElError()
    {
        var resultado = await Ejecutar(nombre: "  ");
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _catalogo.Verify(c => c.VincularAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CA5_CA6_SiLaVinculacionFalla_LaUnidadExteriorRecibeElFallo_ParaDescartarElAlta()
    {
        _catalogo.Setup(c => c.VinculadoAAsync(3, "GN-100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloVinculadoDto(8, "BOM-008", "Bomba de agua"));
        Result? recibido = null;
        _unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<CancellationToken, Task<Result>> operacion, CancellationToken ct) => recibido = await operacion(ct));

        var resultado = await Ejecutar();

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("BOM-008", resultado.Error);
        Assert.NotNull(recibido);
        Assert.Equal(TipoError.Conflicto, recibido!.Tipo);
    }
}

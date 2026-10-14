using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>HU-ART-01 · lógica del caso de uso con dobles. La persistencia se prueba contra PostgreSQL.</summary>
public class CrearArticuloUseCaseTests
{
    private const int IdGenerado = 123;

    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Articulo? _agregado;

    public CrearArticuloUseCaseTests()
    {
        _articulos.Setup(r => r.ObtenerSucursalesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new short[] { 1, 2 });
        _articulos.Setup(r => r.AgregarAsync(It.IsAny<Articulo>(), It.IsAny<CancellationToken>()))
            .Callback((Articulo a, CancellationToken _) => _agregado = a)
            .ReturnsAsync(IdGenerado);
        _articulos.Setup(r => r.AsignarCodigoInternoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _consulta.Setup(c => c.ObtenerConflictosCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ArticuloResumenConsultaDto>());
        Proveedor(10, activo: true, "Distribuidora Norte");
        Proveedor(20, activo: true, "Repuestos Sur");
    }

    private void Proveedor(int id, bool activo, string razonSocial) =>
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor
            {
                Id = id, RazonSocial = razonSocial, Activo = activo,
                MotivoBaja = activo ? null : "Cerró", FechaBaja = activo ? null : DateTime.UtcNow
            });

    private CrearArticuloUseCase Caso() =>
        new(_articulos.Object, _consulta.Object, _proveedores.Object, _unidad.Object);

    private static CrearArticuloRequest Request(params (int ProveedorId, string Codigo)[] codigos) =>
        new("Bujía NGK BPR6ES", "Estante 4", codigos.Select(c => new CodigoProveedorRequest(c.ProveedorId, c.Codigo)).ToArray());

    private void NoSeEscribioNada()
    {
        _articulos.Verify(r => r.AgregarAsync(It.IsAny<Articulo>(), It.IsAny<CancellationToken>()), Times.Never);
        _articulos.Verify(r => r.AsignarCodigoInternoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Validaciones ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreVacio_Invalido(string nombre)
    {
        var resultado = await Caso().EjecutarAsync(new(nombre, null, Array.Empty<CodigoProveedorRequest>()));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task NombreDeMasDe200_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new(new string('a', 201), null, Array.Empty<CodigoProveedorRequest>()));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task UbicacionDeMasDe60_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(new("Bujía", new string('u', 61), Array.Empty<CodigoProveedorRequest>()));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CodigoVacio_Invalido(string codigo)
    {
        var resultado = await Caso().EjecutarAsync(Request((10, codigo)));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task CodigoDeMasDe60_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request((10, new string('C', 61))));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    // ---- D2 y unicidad global ----

    [Fact]
    public async Task DosCodigosDelMismoProveedor_ConflictoD2()
    {
        var resultado = await Caso().EjecutarAsync(Request((10, "GN-01"), (10, "GN-02")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Distribuidora Norte", resultado.Error);
        NoSeEscribioNada();
    }

    // La misma clave normalizada dos veces, de proveedores distintos: la unicidad es global.
    [Fact]
    public async Task MismoCodigoNormalizadoDosVeces_Conflicto()
    {
        var resultado = await Caso().EjecutarAsync(Request((10, " gn-01 "), (20, "GN-01")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("GN-01", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task CodigoDeOtroArticulo_ConflictoInformandoCual()
    {
        _consulta.Setup(c => c.ObtenerConflictosCodigoAsync("GN-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ArticuloResumenConsultaDto(77, "MD14753", "Mouse Genius", true) });

        var resultado = await Caso().EjecutarAsync(Request((10, " gn-01 ")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("MD14753", resultado.Error);
        Assert.Contains("Mouse Genius", resultado.Error);
        NoSeEscribioNada();
    }

    // ---- Proveedor ----

    [Fact]
    public async Task ProveedorInexistente_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request((99, "GN-01")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("99", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ProveedorInactivo_Invalido()
    {
        Proveedor(30, activo: false, "Proveedor Cerrado");

        var resultado = await Caso().EjecutarAsync(Request((30, "GN-01")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("Proveedor Cerrado", resultado.Error);
        NoSeEscribioNada();
    }

    // ---- Alta ----

    [Fact]
    public async Task Alta_ArmaElArticuloCompleto()
    {
        var resultado = await Caso().EjecutarAsync(new("  Bujía NGK BPR6ES ", " Estante 4 ",
            new CodigoProveedorRequest[] { new(10, " bpr-6es "), new(20, "ngk 4321") }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_agregado);
        Assert.Equal("Bujía NGK BPR6ES", _agregado.Nombre);
        Assert.Equal("Estante 4", _agregado.UbicacionDeposito);
        Assert.True(_agregado.Activo);
        Assert.Null(_agregado.CostoVigente);
        // NormalizacionArticulo.Codigo: la misma función con la que se busca.
        Assert.Equal(new[] { ("BPR-6ES", 10), ("NGK 4321", 20) },
            _agregado.CodigosAlternativos.Select(c => (c.Codigo, c.ProveedorId)));
        Assert.Equal(new[] { (1, 0m), (2, 0m) }, _agregado.Existencias.Select(e => (e.SucursalId, e.Cantidad)));
        Assert.Equal(ReglasPrecioModalidad.ConfiguracionInicial(), _agregado.Precios);
    }

    [Fact]
    public async Task Alta_SinUbicacion_GuardaNull()
    {
        var resultado = await Caso().EjecutarAsync(new("Bujía", "   ", Array.Empty<CodigoProveedorRequest>()));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_agregado);
        Assert.Null(_agregado.UbicacionDeposito);
        Assert.Empty(_agregado.CodigosAlternativos);
    }

    // Sin MAX+1: el código sale del Id que generó la base, no de contar los existentes.
    [Fact]
    public async Task Alta_GeneraElCodigoInternoDesdeElIdGenerado()
    {
        var resultado = await Caso().EjecutarAsync(Request((10, "GN-01")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new CrearArticuloResponse(IdGenerado, "LC-000123"), resultado.Value);
        _articulos.Verify(r => r.AsignarCodigoInternoAsync(IdGenerado, "LC-000123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Alta_EscriturasDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(Request((10, "GN-01")));

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task Alta_SinIdParaAsignarCodigo_FallaYNoDevuelveExito()
    {
        _articulos.Setup(r => r.AsignarCodigoInternoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await Caso().EjecutarAsync(Request((10, "GN-01")));

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }
}

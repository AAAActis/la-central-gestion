using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-01 · lógica del caso de uso con dobles. Que la factura y sus efectos se
/// escriban, y que no quede nada si algo falla, se prueba contra PostgreSQL.
/// </summary>
public class RegistrarFacturaCompraUseCaseTests
{
    private const int ProveedorActivo = 10;
    private const int ProveedorInactivo = 11;
    private const int IdGenerado = 501;

    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();
    private FacturaCompra? _registrada;

    public RegistrarFacturaCompraUseCaseTests()
    {
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(ProveedorActivo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = ProveedorActivo, RazonSocial = "Proveedor activo", Activo = true });
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(ProveedorInactivo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor
            {
                Id = ProveedorInactivo, RazonSocial = "Proveedor inactivo", Activo = false,
                MotivoBaja = "Cerró", FechaBaja = DateTime.UtcNow
            });
        _articulos.Setup(a => a.ObtenerSucursalesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new short[] { 1, 2 });
        _facturas.Setup(f => f.ObtenerEstadoArticulosAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool> { [100] = true, [200] = true, [300] = false });
        _facturas.Setup(f => f.ObtenerNumeroActivoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FacturaCompraExistente?)null);
        _facturas.Setup(f => f.RegistrarConEfectosAsync(It.IsAny<FacturaCompra>(), It.IsAny<CancellationToken>()))
            .Callback((FacturaCompra f, CancellationToken _) => _registrada = f)
            .ReturnsAsync(Result<int>.Success(IdGenerado));
    }

    private RegistrarFacturaCompraUseCase Caso() =>
        new(_facturas.Object, _proveedores.Object, _articulos.Object, _unidad.Object, _contexto.Object);

    private static LineaFacturaCompraRequest Linea(int articuloId = 100, short sucursalId = 1,
        decimal cantidad = 5m, decimal precio = 200m, decimal descuento = 10m) =>
        new(articuloId, sucursalId, cantidad, precio, descuento);

    private static RegistrarFacturaCompraRequest Request(
        IReadOnlyList<LineaFacturaCompraRequest>? lineas = null, int proveedorId = ProveedorActivo,
        string numero = " A-0001-00000123 ", DateOnly? emision = null, DateOnly? vencimiento = null,
        decimal iva = 30m, decimal iibb = 4.5m, decimal municipal = 1.25m, decimal ganancias = 0m) =>
        new(proveedorId, numero, emision ?? new DateOnly(2026, 10, 1), vencimiento ?? new DateOnly(2026, 10, 31),
            iva, iibb, municipal, ganancias, lineas ?? new[] { Linea() });

    private void NoSeEscribioNada() =>
        _facturas.Verify(f => f.RegistrarConEfectosAsync(It.IsAny<FacturaCompra>(), It.IsAny<CancellationToken>()), Times.Never);

    private static void AssertFallo(Result resultado, TipoError tipo)
    {
        Assert.False(resultado.IsSuccess);
        Assert.Equal(tipo, resultado.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
    }

    // ---- CA3 · sin líneas ----

    [Fact]
    public async Task SinLineas_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(Array.Empty<LineaFacturaCompraRequest>()));

        AssertFallo(resultado, TipoError.Invalido);
        Assert.Contains("línea", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task LineasAusentes_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request() with { Lineas = null });

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    // ---- Cabecera ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NumeroVacio_Invalido(string numero)
    {
        var resultado = await Caso().EjecutarAsync(Request(numero: numero));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task NumeroDeMasDe30Caracteres_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(numero: new string('9', 31)));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task VencimientoAnteriorALaEmision_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(
            emision: new DateOnly(2026, 10, 10), vencimiento: new DateOnly(2026, 10, 9)));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task VencimientoElMismoDiaDeLaEmision_Registra()
    {
        var resultado = await Caso().EjecutarAsync(Request(
            emision: new DateOnly(2026, 10, 10), vencimiento: new DateOnly(2026, 10, 10)));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new DateOnly(2026, 10, 10), _registrada!.FechaVencimiento);
    }

    [Theory]
    [InlineData(-0.01, 0, 0, 0)]
    [InlineData(0, -1, 0, 0)]
    [InlineData(0, 0, -1, 0)]
    [InlineData(0, 0, 0, -1)]
    [InlineData(0.001, 0, 0, 0)]
    [InlineData(0, 1000000000000, 0, 0)]
    [InlineData(0, 0, 0.005, 0)]
    [InlineData(0, 0, 0, 1000000000000)]
    public async Task PercepcionNegativaOFueraDeLaColumna_Invalido(double iva, double iibb, double municipal, double ganancias)
    {
        var resultado = await Caso().EjecutarAsync(Request(
            iva: (decimal)iva, iibb: (decimal)iibb, municipal: (decimal)municipal, ganancias: (decimal)ganancias));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    // ---- CA1 · proveedor activo ----

    [Fact]
    public async Task ProveedorInexistente_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(proveedorId: 999));

        AssertFallo(resultado, TipoError.Invalido);
        Assert.Equal("El proveedor 999 no existe.", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ProveedorInactivo_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(proveedorId: ProveedorInactivo));

        AssertFallo(resultado, TipoError.Invalido);
        Assert.Contains("inactivo", resultado.Error);
        NoSeEscribioNada();
    }

    // ---- CA2 · número activo repetido para ese proveedor ----

    [Fact]
    public async Task NumeroActivoRepetido_ConflictoQueInformaLaFacturaConLaQueChoca()
    {
        _facturas.Setup(f => f.ObtenerNumeroActivoAsync(ProveedorActivo, "A-0001-00000123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FacturaCompraExistente(77, "A-0001-00000123", new DateOnly(2026, 9, 15)));

        var resultado = await Caso().EjecutarAsync(Request());

        AssertFallo(resultado, TipoError.Conflicto);
        Assert.Contains("77", resultado.Error);
        Assert.Contains("A-0001-00000123", resultado.Error);
        Assert.Contains("15/09/2026", resultado.Error);
        NoSeEscribioNada();
    }

    // ---- Líneas ----

    [Theory]
    [InlineData(0, 200, 0)]
    [InlineData(-1, 200, 0)]
    [InlineData(5, -0.01, 0)]
    [InlineData(5, 200, -0.01)]
    [InlineData(5, 200, 100.01)]
    // Escala y rango de NUMERIC(12,3) / NUMERIC(14,2) / NUMERIC(6,2): lo que la base no guarda es Invalido, no 500.
    [InlineData(0.0004, 200, 0)]
    [InlineData(1000000000, 200, 0)]
    [InlineData(5, 200.001, 0)]
    [InlineData(5, 1000000000000, 0)]
    [InlineData(5, 200, 10.005)]
    public async Task LineaConValoresFueraDeRango_Invalido(double cantidad, double precio, double descuento)
    {
        var resultado = await Caso().EjecutarAsync(Request(new[]
            { Linea(cantidad: (decimal)cantidad, precio: (decimal)precio, descuento: (decimal)descuento) }));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ArticuloRepetidoEnDosLineas_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100, 1), Linea(100, 2) }));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ArticuloInexistente_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100), Linea(999) }));

        AssertFallo(resultado, TipoError.Invalido);
        // Inexistente no es lo mismo que dado de baja: el mensaje no puede pedir reactivarlo.
        Assert.Equal("El artículo 999 no existe.", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ArticuloInactivo_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100), Linea(300) }));

        AssertFallo(resultado, TipoError.Invalido);
        Assert.Contains("300", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task SucursalInexistente_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100, sucursalId: 9) }));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    // ---- Sesión ----

    [Fact]
    public async Task SinUsuarioEnSesion_NoAutorizadoYNoEscribe()
    {
        _contexto = DoblesBaja.Contexto(0);

        var resultado = await Caso().EjecutarAsync(Request());

        AssertFallo(resultado, TipoError.NoAutorizado);
        NoSeEscribioNada();
    }

    // ---- CA1 y CA4 · registro ----

    [Fact]
    public async Task Valida_RegistraLaFacturaConSusDatosYDevuelveElId()
    {
        var antes = DateTime.UtcNow;

        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100, 1, 5m, 200m, 10m), Linea(200, 2, 2.5m, 80m, 0m) }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(IdGenerado, resultado.Value.Id);
        Assert.NotNull(_registrada);
        Assert.Equal(ProveedorActivo, _registrada.ProveedorId);
        Assert.Equal("A-0001-00000123", _registrada.Numero);
        // CA4: emisión, vencimiento y las cuatro percepciones, tal cual.
        Assert.Equal(new DateOnly(2026, 10, 1), _registrada.FechaEmision);
        Assert.Equal(new DateOnly(2026, 10, 31), _registrada.FechaVencimiento);
        Assert.Equal(30m, _registrada.PercepcionIva);
        Assert.Equal(4.5m, _registrada.PercepcionIibb);
        Assert.Equal(1.25m, _registrada.PercepcionMunicipal);
        Assert.Equal(0m, _registrada.PercepcionGanancias);
        Assert.Equal(DoblesBaja.UsuarioEnSesion, _registrada.UsuarioId);
        Assert.InRange(_registrada.FechaRegistro, antes, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, _registrada.FechaRegistro.Kind);
        Assert.Collection(_registrada.Lineas,
            l => Assert.Equal((100, (short)1, 5m, 200m, 10m), (l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje)),
            l => Assert.Equal((200, (short)2, 2.5m, 80m, 0m), (l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje)));
    }

    [Fact]
    public async Task SinVencimientoNiPercepciones_RegistraNullYCeros()
    {
        var resultado = await Caso().EjecutarAsync(Request(iva: 0m, iibb: 0m, municipal: 0m) with { FechaVencimiento = null });

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_registrada);
        Assert.Null(_registrada.FechaVencimiento);
        Assert.Equal(0m, _registrada.PercepcionIva + _registrada.PercepcionIibb + _registrada.PercepcionMunicipal + _registrada.PercepcionGanancias);
    }

    [Fact]
    public async Task RepositorioRechazaElStockDeUnDestino_DevuelveSuFalloParaQueSeDescarteTodo()
    {
        _facturas.Setup(f => f.RegistrarConEfectosAsync(It.IsAny<FacturaCompra>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure(TipoError.Invalido, "El stock no entra."));
        Result? devueltoALaUnidad = null;
        _unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<CancellationToken, Task<Result>> operacion, CancellationToken ct) => devueltoALaUnidad = await operacion(ct));

        var resultado = await Caso().EjecutarAsync(Request());

        // La unidad de trabajo recibe el fallo (y por eso revierte), y el caso de uso lo informa.
        Assert.NotNull(devueltoALaUnidad);
        Assert.False(devueltoALaUnidad.IsSuccess);
        AssertFallo(resultado, TipoError.Invalido);
        Assert.Equal("El stock no entra.", resultado.Error);
    }

    [Fact]
    public async Task Valida_ElRegistroOcurreDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(Request());

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeEscribioNada();
    }
}

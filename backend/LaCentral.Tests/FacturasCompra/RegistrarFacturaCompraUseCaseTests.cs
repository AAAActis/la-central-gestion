using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
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
    private readonly Mock<IPreciosArticuloRepositorio> _precios = new();
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
        // Por defecto los comprados no tienen filas de precio: no hay nada que recalcular.
        _precios.Setup(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, PreciosDeArticulo>());
    }

    private RegistrarFacturaCompraUseCase Caso() =>
        new(_facturas.Object, _proveedores.Object, _articulos.Object, _unidad.Object, _contexto.Object,
            new CalcularPreciosArticuloUseCase(_precios.Object));

    private static LineaFacturaCompraRequest Linea(int articuloId = 100, short sucursalId = 1,
        decimal cantidad = 5m, decimal precio = 200m, decimal descuento = 10m) =>
        new(articuloId, sucursalId, cantidad, precio, descuento);

    private static RegistrarFacturaCompraRequest Request(
        IReadOnlyList<LineaFacturaCompraRequest>? lineas = null, int proveedorId = ProveedorActivo,
        string numero = " A-0001-00000123 ", DateOnly? emision = null, DateOnly? vencimiento = null,
        decimal iva = 30m, decimal iibb = 4.5m, decimal municipal = 1.25m, decimal ganancias = 0m,
        string? tipo = "A", DateOnly? imputacion = null) =>
        new(proveedorId, numero, emision ?? new DateOnly(2026, 10, 1), vencimiento ?? new DateOnly(2026, 10, 31),
            iva, iibb, municipal, ganancias, lineas ?? new[] { Linea() }, tipo, imputacion);

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
    public async Task MismoArticuloYSucursalEnDosLineas_Invalido()
    {
        // La clave del detalle es (factura, artículo, sucursal): dos líneas así chocarían en la base.
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100, 1), Linea(100, 1) }));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task MismoArticuloEnDosSucursales_Registra()
    {
        // D9: el destino es por línea. Con la clave de tres columnas (07), el mismo artículo
        // puede ir a Fragueiro y a San Vicente en la misma factura.
        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100, 1), Linea(100, 2) }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_registrada);
        Assert.Equal(new[] { (100, 1), (100, 2) }, _registrada.Lineas.Select(l => (l.ArticuloId, (int)l.SucursalId)));
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

    // ---- CA5 (HU-ART-05 CA-001): recálculo de precios de los comprados ----

    private void PreciosExistentes(params (int Id, decimal? Costo)[] articulos) =>
        _precios.Setup(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articulos.ToDictionary(a => a.Id,
                a => new PreciosDeArticulo(a.Costo, ReglasPrecioModalidad.ConfiguracionInicial())));

    [Fact]
    public async Task Valida_RecalculaLosPreciosDeLosComprados_ConLaFechaDelRegistro()
    {
        PreciosExistentes((100, 180m), (200, 80m));
        var escritos = new Dictionary<int, IReadOnlyList<PrecioModalidad>>();
        _precios.Setup(p => p.ActualizarPreciosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<PrecioModalidad>>(), It.IsAny<CancellationToken>()))
            .Callback((int id, IReadOnlyList<PrecioModalidad> filas, CancellationToken _) => escritos[id] = filas)
            .Returns(Task.CompletedTask);

        var resultado = await Caso().EjecutarAsync(Request(new[] { Linea(100), Linea(200, sucursalId: 2) }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        _precios.Verify(p => p.ObtenerAsync(
            It.Is<IReadOnlyCollection<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 100, 200 })),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new[] { 100, 200 }, escritos.Keys.Order());
        Assert.Equal(270m, escritos[100].Single(p => p.Modalidad == Modalidad.Lista).Precio);
        Assert.All(escritos.Values.SelectMany(f => f).Where(p => p.Precio is not null),
            p => Assert.Equal(_registrada!.FechaRegistro, p.FechaCalculo));
    }

    [Fact]
    public async Task Valida_ElRecalculoOcurreDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();
        PreciosExistentes((100, 180m));

        await Caso().EjecutarAsync(Request());

        _precios.Verify(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        _precios.Verify(p => p.ActualizarPreciosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<PrecioModalidad>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // El recálculo corre después de escribir la compra: si rechaza, la unidad de trabajo
    // recibe el fallo y descarta también la factura, el stock y el costo.
    [Fact]
    public async Task RecalculoRechazado_DevuelveSuFalloALaUnidadParaQueSeDescarteTodo()
    {
        PreciosExistentes((100, 6_666_666_666.67m));
        Result? devueltoALaUnidad = null;
        _unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns(async (Func<CancellationToken, Task<Result>> operacion, CancellationToken ct) => devueltoALaUnidad = await operacion(ct));

        var resultado = await Caso().EjecutarAsync(Request());

        Assert.NotNull(devueltoALaUnidad);
        Assert.False(devueltoALaUnidad.IsSuccess);
        AssertFallo(resultado, TipoError.Invalido);
        Assert.Contains("artículo 100", resultado.Error);
    }

    [Fact]
    public async Task Valida_ElRegistroOcurreDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(Request());

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeEscribioNada();
    }

    // ---- HU-FCO-06 (056) ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("C")]
    [InlineData("AB")]
    public async Task Fco06_TipoDeComprobanteAusenteODistintoDeAyB_Invalido(string? tipo)
    {
        var resultado = await Caso().EjecutarAsync(Request(tipo: tipo));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData(" b ", "B")]
    public async Task Fco06_TipoDeComprobante_SeRegistraEnLaFactura(string tipo, string esperado)
    {
        var numero = esperado == "B" ? "99-00001234" : "0001-00000123";

        var resultado = await Caso().EjecutarAsync(Request(tipo: tipo, numero: numero));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(esperado, _registrada!.TipoComprobante);
    }

    // CA-005 (D-S6-10): el B lleva 99 en el primer tramo; el 999 no se usa.
    [Theory]
    [InlineData("0001-00001234")]
    [InlineData("999-00001234")]
    [InlineData("099-00001234")]
    [InlineData("9900001234")]
    public async Task Fco06_ComprobanteB_SinNoventaYNueveEnElPrimerTramo_Invalido(string numero)
    {
        var resultado = await Caso().EjecutarAsync(Request(tipo: "B", numero: numero));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    // CA-005: el A se copia tal cual, cualquier combinación.
    [Theory]
    [InlineData("99-00001234")]
    [InlineData("999-1")]
    [InlineData("0001-00000123")]
    [InlineData("A-12")]
    public async Task Fco06_ComprobanteA_SeCopiaElNumeroExacto(string numero)
    {
        var resultado = await Caso().EjecutarAsync(Request(tipo: "A", numero: numero));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(numero, _registrada!.Numero);
    }

    // CA-006
    [Fact]
    public async Task Fco06_FechaDeImputacion_SeGuardaAparte()
    {
        var resultado = await Caso().EjecutarAsync(Request(imputacion: new DateOnly(2026, 11, 1)));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new DateOnly(2026, 10, 1), _registrada!.FechaEmision);
        Assert.Equal(new DateOnly(2026, 11, 1), _registrada.FechaImputacion);
    }

    [Fact]
    public async Task Fco06_SinFechaDeImputacion_EsLaEmision()
    {
        var resultado = await Caso().EjecutarAsync(Request(emision: new DateOnly(2026, 10, 5)));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new DateOnly(2026, 10, 5), _registrada!.FechaImputacion);
    }

    // CA-012
    [Fact]
    public async Task Fco06_TresDescuentos_SeRegistranEnLaLinea()
    {
        var resultado = await Caso().EjecutarAsync(Request(new[] { new LineaFacturaCompraRequest(100, 1, 5m, 200m, 10m, 5m, 2.5m) }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        var linea = Assert.Single(_registrada!.Lineas);
        Assert.Equal((10m, 5m, 2.5m), (linea.DescuentoPorcentaje, linea.Descuento2Porcentaje, linea.Descuento3Porcentaje));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(100.01, 0)]
    [InlineData(0, -0.01)]
    [InlineData(0, 101)]
    [InlineData(0.001, 0)]
    [InlineData(0, 0.001)]
    public async Task Fco06_SegundoOTercerDescuentoFueraDeRango_Invalido(double segundo, double tercero)
    {
        var resultado = await Caso().EjecutarAsync(Request(new[]
            { new LineaFacturaCompraRequest(100, 1, 5m, 200m, 0m, (decimal)segundo, (decimal)tercero) }));

        AssertFallo(resultado, TipoError.Invalido);
        NoSeEscribioNada();
    }

    // CA-014 y CA-015: neto con descuentos sucesivos, 21 % de IVA solo en el A, percepciones, total.
    [Fact]
    public void Fco06_Totales_ComprobanteA_SumaVeintiunoPorCientoDeIva()
    {
        // 5 × 180 (200 − 10 %) = 900; 2,5 × 80 = 200 → neto 1100; IVA 231; percepciones 35,75.
        var resultado = Caso().CalcularTotales(Request(new[] { Linea(100, 1, 5m, 200m, 10m), Linea(200, 2, 2.5m, 80m, 0m) }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new TotalesFacturaCompra(1100m, 231m, 35.75m, 1366.75m), resultado.Value);
    }

    [Fact]
    public void Fco06_Totales_ComprobanteB_NoSumaIva()
    {
        var resultado = Caso().CalcularTotales(Request(new[] { Linea(100, 1, 5m, 200m, 10m), Linea(200, 2, 2.5m, 80m, 0m) },
            tipo: "B", numero: "99-00000001"));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new TotalesFacturaCompra(1100m, 0m, 35.75m, 1135.75m), resultado.Value);
    }

    [Fact]
    public void Fco06_Totales_ElIvaSeRedondeaACentavos()
    {
        // neto 0,15 × 1 = 0,15; IVA 0,0315 → 0,03.
        var resultado = Caso().CalcularTotales(Request(new[] { Linea(100, 1, 1m, 0.15m, 0m) }, iva: 0m, iibb: 0m, municipal: 0m));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new TotalesFacturaCompra(0.15m, 0.03m, 0m, 0.18m), resultado.Value);
    }

    [Fact]
    public void Fco06_Totales_LasPercepcionesIncluyenGanancias()
    {
        // 30 + 4,5 + 1,25 + 2 = 37,75: las cuatro percepciones (D8).
        var resultado = Caso().CalcularTotales(Request(new[] { Linea(100, 1, 5m, 200m, 10m) }, ganancias: 2m));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new TotalesFacturaCompra(900m, 189m, 37.75m, 1126.75m), resultado.Value);
    }

    [Fact]
    public void Fco06_Totales_CadaLineaSeRedondeaACentavosAntesDeSumar()
    {
        // 1,005 × 1,00 = 1,005 → 1,01 por línea; dos líneas = 2,02 (sumar sin redondear daría 2,01).
        var resultado = Caso().CalcularTotales(Request(new[] { Linea(100, 1, 1.005m, 1m, 0m), Linea(200, 1, 1.005m, 1m, 0m) },
            tipo: "B", numero: "99-00000001", iva: 0m, iibb: 0m, municipal: 0m));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new TotalesFacturaCompra(2.02m, 0m, 0m, 2.02m), resultado.Value);
    }

    [Fact]
    public void Fco06_Totales_ConDatosInvalidos_Invalido()
    {
        var resultado = Caso().CalcularTotales(Request(tipo: null));

        AssertFallo(resultado, TipoError.Invalido);
    }
}

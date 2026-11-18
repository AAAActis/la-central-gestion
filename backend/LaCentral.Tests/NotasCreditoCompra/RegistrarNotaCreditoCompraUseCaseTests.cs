using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.NotasCreditoCompra;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>
/// HU-FCO-07 (057) · lógica del caso de uso con dobles. Que la nota, su relación con las
/// facturas y el stock se escriban, y que el costo no cambie, se prueba contra PostgreSQL.
/// </summary>
public class RegistrarNotaCreditoCompraUseCaseTests
{
    private const int ProveedorActivo = 10;
    private const int ProveedorInactivo = 11;
    private const int OtroProveedor = 99;
    private const int IdGenerado = 701;
    private const short Reclamos = 3;

    private readonly Mock<INotaCreditoCompraRepositorio> _notas = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();
    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();
    private NotaCreditoCompra? _registrada;

    private static readonly FacturaParaNotaCredito[] Facturas =
    [
        new(501, ProveedorActivo, true),
        new(502, ProveedorActivo, true),
        new(503, OtroProveedor, true),
        new(504, ProveedorActivo, false),
        new(505, ProveedorInactivo, true)
    ];

    public RegistrarNotaCreditoCompraUseCaseTests()
    {
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(ProveedorActivo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = ProveedorActivo, RazonSocial = "Proveedor activo", Activo = true });
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(ProveedorInactivo, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor
            {
                Id = ProveedorInactivo, RazonSocial = "Proveedor inactivo", Activo = false,
                MotivoBaja = "Cerró", FechaBaja = DateTime.UtcNow
            });
        _notas.Setup(n => n.ObtenerFacturasAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) => Facturas.Where(f => ids.Contains(f.Id)).ToList());
        _notas.Setup(n => n.ExisteNumeroAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _notas.Setup(n => n.RegistrarConEfectosAsync(It.IsAny<NotaCreditoCompra>(), It.IsAny<CancellationToken>()))
            .Callback((NotaCreditoCompra n, CancellationToken _) => _registrada = n)
            .ReturnsAsync(Result<int>.Success(IdGenerado));
        _facturas.Setup(f => f.ObtenerEstadoArticulosAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, bool> { [100] = true, [200] = true, [300] = false });
        _articulos.Setup(a => a.ObtenerSucursalesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new short[] { 1, 2, Reclamos });
    }

    private RegistrarNotaCreditoCompraUseCase Caso() =>
        new(_notas.Object, _proveedores.Object, _facturas.Object, _articulos.Object, _unidad.Object, _contexto.Object);

    private static LineaNotaCreditoCompraRequest Linea(int articuloId = 100, short origen = 1, decimal cantidad = 2m) =>
        new(articuloId, origen, cantidad);

    private static RegistrarNotaCreditoCompraRequest Bonificacion(
        int proveedorId = ProveedorActivo, string numero = " 0010-00021495 ", decimal importe = 115040m,
        decimal? iva = null, decimal? iibb = null, IReadOnlyList<int>? facturas = null,
        IReadOnlyList<LineaNotaCreditoCompraRequest>? lineas = null, string? clase = "BONIFICACION") =>
        new(proveedorId, numero, new DateOnly(2026, 11, 5), clase, importe, iva, iibb, null, null,
            facturas ?? [501], lineas);

    private static RegistrarNotaCreditoCompraRequest Devolucion(IReadOnlyList<LineaNotaCreditoCompraRequest>? lineas) =>
        Bonificacion(clase: "DEVOLUCION", lineas: lineas);

    private void NoSeEscribioNada() =>
        _notas.Verify(n => n.RegistrarConEfectosAsync(It.IsAny<NotaCreditoCompra>(), It.IsAny<CancellationToken>()), Times.Never);

    private async Task Rechaza(RegistrarNotaCreditoCompraRequest request, TipoError tipo)
    {
        var resultado = await Caso().EjecutarAsync(request);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(tipo, resultado.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
        NoSeEscribioNada();
    }

    // ---- CA-003 y CA-004 · bonificación ----

    [Fact]
    public async Task Bonificacion_SeRegistraConElNumeroExacto_SinLineas_YConSusFacturas()
    {
        var resultado = await Caso().EjecutarAsync(Bonificacion(iva: 1.5m, facturas: [501, 502]));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(IdGenerado, resultado.Value.Id);
        Assert.NotNull(_registrada);
        Assert.Equal((ProveedorActivo, "0010-00021495", NotaCreditoCompra.Bonificacion, 115040m, (decimal?)1.5m),
            (_registrada.ProveedorId, _registrada.Numero, _registrada.Clase, _registrada.ImporteNeto, _registrada.PercepcionIva));
        Assert.Equal(new DateOnly(2026, 11, 5), _registrada.FechaEmision);
        Assert.Equal([501, 502], _registrada.FacturaIds);
        Assert.Empty(_registrada.Lineas);
        Assert.Equal(DoblesBaja.UsuarioEnSesion, _registrada.UsuarioId);
        Assert.False(_registrada.DesdePdf);
    }

    [Fact]
    public async Task LaClaseSeNormaliza_MinusculasYEspacios()
    {
        var resultado = await Caso().EjecutarAsync(Bonificacion(clase: " bonificacion "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(NotaCreditoCompra.Bonificacion, _registrada?.Clase);
    }

    [Fact]
    public async Task Bonificacion_ConLineas_Invalido_PorqueNoMueveStock()
        => await Rechaza(Bonificacion(lineas: [Linea()]), TipoError.Invalido);

    // ---- CA-005 · devolución ----

    [Fact]
    public async Task Devolucion_RegistraCadaLineaConSuOrigen_ElMismoArticuloPuedeSalirDeDosOrigenes()
    {
        var resultado = await Caso().EjecutarAsync(Devolucion([Linea(100, 1, 2m), Linea(100, Reclamos, 1.5m), Linea(200, 2, 1m)]));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(NotaCreditoCompra.Devolucion, _registrada?.Clase);
        Assert.Equal([(100, (short)1, 2m), (100, Reclamos, 1.5m), (200, (short)2, 1m)],
            _registrada!.Lineas.Select(l => (l.ArticuloId, l.SucursalId, l.Cantidad)));
    }

    [Fact]
    public async Task Devolucion_DeUnArticuloDadoDeBaja_SeRegistra()
    {
        // Se devuelve lo que se compró, aunque después se haya dado de baja.
        var resultado = await Caso().EjecutarAsync(Devolucion([Linea(300)]));

        Assert.True(resultado.IsSuccess, resultado.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Devolucion_SinLineas_Invalido(bool vacia)
        => await Rechaza(Devolucion(vacia ? [] : null), TipoError.Invalido);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.0001)]   // NUMERIC(12,3)
    [InlineData(1000000000)]
    public async Task Devolucion_CantidadQueNoEntra_Invalido(decimal cantidad)
        => await Rechaza(Devolucion([Linea(cantidad: cantidad)]), TipoError.Invalido);

    [Fact]
    public async Task Devolucion_ArticuloInexistente_Invalido()
        => await Rechaza(Devolucion([Linea(articuloId: 999)]), TipoError.Invalido);

    [Fact]
    public async Task Devolucion_OrigenInexistente_Invalido()
        => await Rechaza(Devolucion([Linea(origen: 9)]), TipoError.Invalido);

    [Fact]
    public async Task Devolucion_ElMismoArticuloDosVecesDelMismoOrigen_Invalido()
        => await Rechaza(Devolucion([Linea(100, 1, 1m), Linea(100, 1, 2m)]), TipoError.Invalido);

    // ---- CA-001 y CA-002 · facturas del mismo proveedor ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SinFacturas_Invalido(bool vacia)
        => await Rechaza(Bonificacion() with { FacturaIds = vacia ? [] : null }, TipoError.Invalido);

    [Theory]
    [InlineData(503)]   // de otro proveedor
    [InlineData(504)]   // anulada
    [InlineData(999)]   // no existe
    public async Task UnaFacturaQueNoSirve_Invalido_AunqueLasOtrasSirvan(int factura)
        => await Rechaza(Bonificacion(facturas: [501, factura]), TipoError.Invalido);

    [Fact]
    public async Task LaMismaFacturaDosVeces_Invalido()
        => await Rechaza(Bonificacion(facturas: [501, 501]), TipoError.Invalido);

    // ---- CA-003 · número ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0010-000214950000000000000000001")]   // 31: numero es VARCHAR(30)
    public async Task NumeroVacioOLargo_Invalido(string numero)
        => await Rechaza(Bonificacion(numero: numero), TipoError.Invalido);

    [Fact]
    public async Task NumeroRepetidoDelMismoProveedor_Conflicto()
    {
        _notas.Setup(n => n.ExisteNumeroAsync(ProveedorActivo, "0010-00021495", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Rechaza(Bonificacion(), TipoError.Conflicto);
    }

    // ---- Cabecera ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("DEVOLUCIONES")]
    public async Task ClaseQueNoEs_BonificacionNiDevolucion_Invalido(string? clase)
        => await Rechaza(Bonificacion(clase: clase), TipoError.Invalido);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10.001)]                  // NUMERIC(14,2)
    [InlineData(1000000000000)]
    public async Task ImporteQueNoEntra_Invalido(decimal importe)
        => await Rechaza(Bonificacion(importe: importe), TipoError.Invalido);

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.001)]
    public async Task PercepcionQueNoEntra_Invalido(decimal percepcion)
    {
        await Rechaza(Bonificacion(iva: percepcion), TipoError.Invalido);
        await Rechaza(Bonificacion(iibb: percepcion), TipoError.Invalido);
        await Rechaza(Bonificacion() with { PercepcionMunicipal = percepcion }, TipoError.Invalido);
        await Rechaza(Bonificacion() with { PercepcionGanancias = percepcion }, TipoError.Invalido);
    }

    [Fact]
    public async Task ProveedorInexistente_Invalido()
        => await Rechaza(Bonificacion(proveedorId: 12345), TipoError.Invalido);

    [Fact]
    public async Task ProveedorInactivo_Invalido_AunqueLaFacturaSeaSuya()
        => await Rechaza(Bonificacion(proveedorId: ProveedorInactivo, facturas: [505]), TipoError.Invalido);

    [Fact]
    public async Task LasCuatroPercepcionesYElOrigenPdf_LleganALaNota()
    {
        var resultado = await Caso().EjecutarAsync(Bonificacion(iva: 1m, iibb: 2m) with
        {
            PercepcionMunicipal = 3m, PercepcionGanancias = 4m, DesdePdf = true
        });

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(((decimal?)1m, (decimal?)2m, (decimal?)3m, (decimal?)4m, true),
            (_registrada?.PercepcionIva, _registrada?.PercepcionIibb, _registrada?.PercepcionMunicipal,
             _registrada?.PercepcionGanancias, _registrada?.DesdePdf ?? false));
    }

    [Fact]
    public async Task SinUsuarioEnLaSesion_NoAutorizado()
    {
        _contexto = DoblesBaja.Contexto(0);

        await Rechaza(Bonificacion(), TipoError.NoAutorizado);
    }

    [Fact]
    public async Task SiElRepositorioRechaza_DevuelveSuError()
    {
        _notas.Setup(n => n.RegistrarConEfectosAsync(It.IsAny<NotaCreditoCompra>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Failure(TipoError.Conflicto, "ncc_numero_unico"));

        var resultado = await Caso().EjecutarAsync(Bonificacion());

        Assert.Equal((false, TipoError.Conflicto), (resultado.IsSuccess, resultado.Tipo));
    }

    // ---- CA-007 · totales antes de confirmar ----

    [Fact]
    public void Totales_ElDeLaNotaRealDeExpoyer_NetoMasVeintiunoPorCientoDeIva()
    {
        var totales = Caso().CalcularTotales(Bonificacion(importe: 115040m));

        Assert.True(totales.IsSuccess, totales.Error);
        Assert.Equal(new TotalesNotaCreditoCompra(115040m, 24158.40m, 0m, 139198.40m), totales.Value);
    }

    [Fact]
    public void Totales_SumaLasPercepciones_YRedondeaElIvaACentavos()
    {
        var totales = Caso().CalcularTotales(Bonificacion(importe: 10.50m, iva: 1m, iibb: 0.5m) with
        {
            PercepcionMunicipal = 0.25m, PercepcionGanancias = 0.1m
        });

        // 10,50 × 0,21 = 2,205: el medio centavo va hacia arriba, 2,21 (como el IVA de la factura).
        Assert.Equal(new TotalesNotaCreditoCompra(10.50m, 2.21m, 1.85m, 14.56m), totales.Value);
    }

    [Fact]
    public void Totales_DeUnaNotaInvalida_Invalido()
    {
        var totales = Caso().CalcularTotales(Bonificacion(importe: 0m));

        Assert.Equal((false, TipoError.Invalido), (totales.IsSuccess, totales.Tipo));
    }
}

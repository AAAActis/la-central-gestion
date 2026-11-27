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
/// HU-FCO-04 (028), lógica del caso de uso con Moq. Anula quien está en la sesión, si es
/// ADMINISTRADOR, sin autorización de otra persona (CA-006 y CA-007 v1.2, D-S7-04).
/// </summary>
public class AnularFacturaCompraUseCaseTests
{
    private const int FacturaActiva = 40;
    private const int FacturaAnulada = 41;

    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();
    private readonly Mock<IPreciosArticuloRepositorio> _precios = new();
    private readonly Mock<INotaCreditoCompraRepositorio> _notas = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();
    private AnulacionFacturaCompra? _anulacion;

    public AnularFacturaCompraUseCaseTests()
    {
        _facturas.Setup(f => f.ObtenerParaAnularAsync(FacturaActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FacturaCompraParaAnular(FacturaActiva, "A-0001-00000001", true, new[] { 100, 200 }));
        _facturas.Setup(f => f.ObtenerParaAnularAsync(FacturaAnulada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FacturaCompraParaAnular(FacturaAnulada, "A-0001-00000002", false, new[] { 100 }));
        _facturas.Setup(f => f.AnularConEfectosAsync(It.IsAny<AnulacionFacturaCompra>(), It.IsAny<CancellationToken>()))
            .Callback((AnulacionFacturaCompra a, CancellationToken _) => _anulacion = a)
            .ReturnsAsync(Result.Success());
        _notas.Setup(n => n.ObtenerVigentesDeFacturaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _precios.Setup(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, PreciosDeArticulo>());
        _contexto.SetupGet(c => c.EsAdministrador).Returns(true);
    }


    private AnularFacturaCompraUseCase Caso() =>
        new(_facturas.Object, _unidad.Object, _contexto.Object,
            new CalcularPreciosArticuloUseCase(_precios.Object), _notas.Object);

    private static AnularFacturaCompraRequest Pedido(string? motivo = " Cargada dos veces ") => new(motivo);

    private void NoSeAnulo() =>
        _facturas.Verify(f => f.AnularConEfectosAsync(It.IsAny<AnulacionFacturaCompra>(), It.IsAny<CancellationToken>()), Times.Never);

    private void AssertFallo(Result resultado, TipoError tipo)
    {
        Assert.False(resultado.IsSuccess);
        Assert.Equal(tipo, resultado.Tipo);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
        NoSeAnulo();
    }

    // CA-001: el motivo es obligatorio.
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task SinMotivo_Invalido(string? motivo)
        => AssertFallo(await Caso().EjecutarAsync(FacturaActiva, Pedido(motivo: motivo)), TipoError.Invalido);

    [Fact]
    public async Task SinUsuarioEnLaSesion_NoAutorizado()
    {
        _contexto = DoblesBaja.Contexto(0);
        AssertFallo(await Caso().EjecutarAsync(FacturaActiva, Pedido()), TipoError.NoAutorizado);
    }

    [Fact]
    public async Task FacturaInexistente_NoEncontrado()
        => AssertFallo(await Caso().EjecutarAsync(999, Pedido()), TipoError.NoEncontrado);

    // Precondición de la 028: la factura está activa.
    [Fact]
    public async Task FacturaYaAnulada_Conflicto()
        => AssertFallo(await Caso().EjecutarAsync(FacturaAnulada, Pedido()), TipoError.Conflicto);

    // CA-006 (v1.2, D-S7-04): un ADMINISTRADOR anula solo, sin autorización de otra persona.
    [Fact]
    public async Task CA006_UnAdministradorAnulaSolo_ConMotivoYQuienAnulo()
    {
        var resultado = await Caso().EjecutarAsync(FacturaActiva, new AnularFacturaCompraRequest(" Cargada dos veces "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal((FacturaActiva, "Cargada dos veces", DoblesBaja.UsuarioEnSesion),
            (_anulacion!.FacturaId, _anulacion.Motivo, _anulacion.UsuarioId));
        Assert.True((DateTime.UtcNow - _anulacion.Fecha).Duration() < TimeSpan.FromMinutes(1));
    }

    // CA-007 (v1.2, D-S7-04): un OPERADOR recibe 403 y no cambia nada.
    [Fact]
    public async Task CA007_UnOperador_Prohibido()
    {
        _contexto = DoblesBaja.Contexto(10);
        AssertFallo(await Caso().EjecutarAsync(FacturaActiva, Pedido()), TipoError.Prohibido);
    }

    // CA-001: los precios por modalidad se recalculan con el costo devuelto, en la misma unidad de trabajo.
    [Fact]
    public async Task RecalculaLosPreciosDeLosArticulosDeLaFactura_DentroDeLaUnidadDeTrabajo()
    {
        var resultado = await Caso().EjecutarAsync(FacturaActiva, Pedido());

        Assert.True(resultado.IsSuccess, resultado.Error);
        _precios.Verify(p => p.ObtenerAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.OrderBy(i => i).SequenceEqual(new[] { 100, 200 })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SinUnidadDeTrabajo_NoAnulaNada()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(FacturaActiva, Pedido());

        NoSeAnulo();
    }

    // CA-005: si el repositorio rechaza, el fallo sube y no se recalcula nada.
    [Fact]
    public async Task SiLaAnulacionFalla_DevuelveElFalloYNoRecalcula()
    {
        _facturas.Setup(f => f.AnularConEfectosAsync(It.IsAny<AnulacionFacturaCompra>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(TipoError.Invalido, "El stock no entra en su columna."));

        var resultado = await Caso().EjecutarAsync(FacturaActiva, Pedido());

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Equal("El stock no entra en su columna.", resultado.Error);
        _precios.Verify(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // CA-011 (D-S6-22): con notas de crédito vigentes no se anula; el mensaje dice que primero
    // se anulan las notas y las nombra. Se consulta dentro de la unidad de trabajo (con la
    // factura bloqueada), con la factura pedida.
    [Fact]
    public async Task ConNotasDeCreditoVigentes_ConflictoQueLasNombra()
    {
        int? consultada = null;
        _notas.Setup(n => n.ObtenerVigentesDeFacturaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((int id, CancellationToken _) => consultada = id)
            .ReturnsAsync(["0010-00021495", "0010-00021496"]);

        var resultado = await Caso().EjecutarAsync(FacturaActiva, Pedido());

        AssertFallo(resultado, TipoError.Conflicto);
        Assert.Equal(FacturaActiva, consultada);
        Assert.Contains("0010-00021495", resultado.Error);
        Assert.Contains("0010-00021496", resultado.Error);
        Assert.Contains("primero", resultado.Error);
        _precios.Verify(p => p.ObtenerAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LasNotasVigentesSeConsultanDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(FacturaActiva, Pedido());

        _notas.Verify(n => n.ObtenerVigentesDeFacturaAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _unidad = DoblesBaja.UnidadQueEjecuta();
        Assert.True((await Caso().EjecutarAsync(FacturaActiva, Pedido())).IsSuccess);
        _notas.Verify(n => n.ObtenerVigentesDeFacturaAsync(FacturaActiva, It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-011, el borde: con una sola nota vigente ya no se anula.
    [Fact]
    public async Task ConUnaSolaNotaVigente_Conflicto()
    {
        _notas.Setup(n => n.ObtenerVigentesDeFacturaAsync(FacturaActiva, It.IsAny<CancellationToken>())).ReturnsAsync(["0010-00021495"]);

        var resultado = await Caso().EjecutarAsync(FacturaActiva, Pedido());

        AssertFallo(resultado, TipoError.Conflicto);
        Assert.Contains("0010-00021495", resultado.Error);
    }
}

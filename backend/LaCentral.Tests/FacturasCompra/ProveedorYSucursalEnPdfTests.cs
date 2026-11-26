using LaCentral.Api.Pdf;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// Fix E · el proveedor por razón social del layout (una única coincidencia, siempre «Revisar») y la
/// sucursal destinataria, sin números de cuenta y sin adivinar. Textos sintéticos con la estructura
/// de los tres PDF reales; ningún dato real.
/// </summary>
public class ProveedorYSucursalEnPdfTests
{
    private readonly Mock<ILectorTextoPdf> _lector = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();

    public ProveedorYSucursalEnPdfTests()
    {
        _proveedores.Setup(p => p.BuscarActivosEnNegroPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        _facturas.Setup(f => f.ObtenerSucursalIdPorCodigoAsync("FR", It.IsAny<CancellationToken>())).ReturnsAsync((short?)1);
        _facturas.Setup(f => f.ObtenerSucursalIdPorCodigoAsync("SV", It.IsAny<CancellationToken>())).ReturnsAsync((short?)2);
    }

    private async Task<FacturaPdfInterpretada> Interpretar(byte[] pdf, int? proveedorElegido = null)
    {
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(new LectorTextoPdf().LeerRenglones(pdf));
        var r = await new InterpretarFacturaPdfUseCase(_lector.Object, _proveedores.Object, _facturas.Object)
            .EjecutarAsync([1], null, proveedorElegido);
        Assert.True(r.IsSuccess, r.Error);
        return r.Value;
    }

    private void LaRazonSocialDa(string termino, params Proveedor[] proveedores)
        => _proveedores.Setup(p => p.BuscarActivosEnNegroPorRazonSocialAsync(termino, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(proveedores);

    // --- Proveedor ---------------------------------------------------------------------------------

    [Fact]
    public async Task RazonSocialUnica_SeAsigna_YSiempreQuedaParaRevisar()
    {
        LaRazonSocialDa("Fazio", new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(PdfSintetico.PresupuestoFazio());

        Assert.Equal(9, f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
        _facturas.Verify(x => x.ObtenerArticulosPorCodigoProveedorAsync(9, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LaBusquedaPorRazonSocial_PideAlMenosDos_ParaPoderVerLaAmbiguedad()
    {
        LaRazonSocialDa("Fazio", new Proveedor { Id = 9, Activo = true });

        await Interpretar(PdfSintetico.PresupuestoFazio());

        // Con un tope de 1, dos proveedores iguales parecerían una única coincidencia.
        _proveedores.Verify(p => p.BuscarActivosEnNegroPorRazonSocialAsync("Fazio", It.Is<int>(limite => limite >= 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RazonSocialConDosCoincidencias_NoAsignaNada()
    {
        LaRazonSocialDa("Fazio", new Proveedor { Id = 9, Activo = true }, new Proveedor { Id = 10, Activo = true });

        var f = await Interpretar(PdfSintetico.PresupuestoFazio());

        Assert.Null(f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
        _facturas.Verify(x => x.ObtenerArticulosPorCodigoProveedorAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RazonSocialSinCoincidencia_NoAsignaNada()
    {
        var f = await Interpretar(PdfSintetico.PresupuestoFazio());

        Assert.Null(f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
    }

    [Fact]
    public async Task UnCoincidenteInactivo_SeIgnora()
    {
        LaRazonSocialDa("Fazio", new Proveedor { Id = 9, Activo = false });

        var f = await Interpretar(PdfSintetico.PresupuestoFazio());

        Assert.Null(f.ProveedorId);
    }

    // Un proveedor asignado por CUIT implica un CUIT conocido: esa sola condición impide buscar por razón social.
    [Fact]
    public async Task ElCuitManda_SobreLaRazonSocial_YNoQuedaParaRevisar()
    {
        _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitProveedor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 7, Activo = true });
        LaRazonSocialDa("ETMAN", new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(PdfSintetico.FacturaEtman(codigoAfip: "06"));

        Assert.Equal(7, f.ProveedorId);
        Assert.DoesNotContain("proveedorId", f.ParaRevisar);
        _proveedores.Verify(p => p.BuscarActivosEnNegroPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnCuitQueEsDeUnProveedorInactivo_NoSeReemplazaPorOtroPorRazonSocial()
    {
        _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitProveedor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 7, Activo = false });
        LaRazonSocialDa("ETMAN", new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(PdfSintetico.FacturaEtman(codigoAfip: "06"));

        Assert.Null(f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
    }

    [Fact]
    public async Task ElProveedorElegidoAMano_MandaSobreTodo()
    {
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Proveedor { Id = 5, Activo = true });
        LaRazonSocialDa("Fazio", new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(PdfSintetico.PresupuestoFazio(), proveedorElegido: 5);

        Assert.Equal(5, f.ProveedorId);
        _proveedores.Verify(p => p.BuscarActivosEnNegroPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Ningún layout reconocido llega sin razón social; un texto sin layout corta antes
    // (InterpretarFacturaPdfUseCaseTests.UnLayoutQueNoSeConoce_Invalido_YPideLaCargaManual).
    [Theory]
    [InlineData("etman")]
    [InlineData("fazio")]
    [InlineData("expoyer")]
    public async Task CadaLayoutAportaSuRazonSocial(string layout)
    {
        var (pdf, termino) = layout switch
        {
            "etman" => (PdfSintetico.FacturaEtman(codigoAfip: "06"), "ETMAN"),
            "fazio" => (PdfSintetico.PresupuestoFazio(), "Fazio"),
            _ => (PdfSintetico.FacturaExpoyer(codigoAfip: "6"), "Expoyer"),
        };
        // Un solo proveedor, y solo para el término correcto: cualquier otro no encuentra nada.
        LaRazonSocialDa(termino, new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(pdf);

        Assert.Equal(9, f.ProveedorId);
    }

    // D-S7-12: sin CUIT es compra en negro, y una factura A no lo es.
    [Theory]
    [InlineData("etman")]
    [InlineData("expoyer")]
    public async Task UnaFacturaA_SinCuitConocido_NoSeAsignaPorRazonSocial(string layout)
    {
        var (pdf, termino) = layout == "etman"
            ? (PdfSintetico.FacturaEtman(codigoAfip: "01"), "ETMAN")
            : (PdfSintetico.FacturaExpoyer(codigoAfip: "1"), "Expoyer");
        LaRazonSocialDa(termino, new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(pdf);

        Assert.Equal("A", f.TipoComprobante);
        Assert.Null(f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
        _proveedores.Verify(p => p.BuscarActivosEnNegroPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UnComprobanteDeTipoNoReconocido_NoEsA_YSeAsignaPorRazonSocial()
    {
        LaRazonSocialDa("ETMAN", new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar(PdfSintetico.FacturaEtman(codigoAfip: "99"));

        Assert.Null(f.TipoComprobante);
        Assert.Equal(9, f.ProveedorId);
    }

    // --- Sucursal ----------------------------------------------------------------------------------

    [Fact]
    public async Task Etman_MandaLaDireccionDeEntrega_NoElDomicilioDelCliente()
    {
        var f = await Interpretar(PdfSintetico.FacturaEtman(domicilio: "MARIANO FRAGUEIRO 3401 DTO 2", dirEntrega: "SAN JERONIMO 3029"));

        Assert.Equal((short?)2, f.SucursalSugeridaId);
        Assert.Contains("sucursalSugeridaId", f.ParaRevisar);
    }

    [Fact]
    public async Task Etman_SinDireccionDeEntrega_VaElBloqueDelCliente()
    {
        var f = await Interpretar(PdfSintetico.FacturaEtman(domicilio: "MARIANO FRAGUEIRO 3401 DTO 2"));

        Assert.Equal((short?)1, f.SucursalSugeridaId);
    }

    [Fact]
    public async Task Fazio_LeeElClienteYSuDomicilio_HastaElTipoDeOperacion()
    {
        var f = await Interpretar(PdfSintetico.PresupuestoFazio(clienteExtra: "ex multirep fragueiro", domicilio: "MARIANO FRAGUEIRO 3401 LOCAL 2 / CORDOBA"));

        Assert.Equal((short?)1, f.SucursalSugeridaId);
        Assert.Contains("sucursalSugeridaId", f.ParaRevisar);
    }

    [Fact]
    public async Task Fazio_UnaSucursalNombradaDespuesDelTipoDeOperacion_NoCuenta()
    {
        var f = await Interpretar(PdfSintetico.Generar(
            [(20, "*** Presupuesto")],
            [(20, "Cliente: 9999 - CLIENTE DE PRUEBA S.A.S."), (400, "Fecha : 05/02/2027")],
            [(20, "CALLE FALSA 123")],
            [(20, "Tipo Operacion: Cuenta Corriente")],
            [(20, "Observacion: retira en SAN VICENTE")],
            [(20, "Ord."), (40, "Cod. de Fab."), (100, "Descrip."), (250, "Cant."), (280, "Remito"),
              (330, "P. Lista"), (380, "% Desc."), (470, "P. Final"), (530, "Total")]));

        Assert.Null(f.SucursalSugeridaId);
    }

    [Fact]
    public async Task Expoyer_LeeElRenglonDelCliente_YElSiguiente()
    {
        var f = await Interpretar(PdfSintetico.FacturaExpoyer(domicilio: "SAN JERONIMO 3029"));

        Assert.Equal((short?)2, f.SucursalSugeridaId);
    }

    [Fact]
    public async Task Expoyer_UnaSucursalNombradaEnUnArticulo_NoCuenta()
    {
        var f = await Interpretar(PdfSintetico.FacturaExpoyer(primerArticulo: "FILTRO SAN VICENTE"));

        Assert.Null(f.SucursalSugeridaId);
    }

    [Fact]
    public async Task Etman_UnaSucursalNombradaFueraDelBloqueDelCliente_NoCuenta()
    {
        var f = await Interpretar(PdfSintetico.FacturaEtman(remitidoPor: "SAN VICENTE"));

        Assert.Null(f.SucursalSugeridaId);
    }

    [Fact]
    public async Task ConLasDosSucursalesEnElDestinatario_NoSugiereNada_NiLaBusca()
    {
        var f = await Interpretar(PdfSintetico.PresupuestoFazio(domicilio: "FRAGUEIRO 3401 Y SAN VICENTE 100"));

        Assert.Null(f.SucursalSugeridaId);
        Assert.DoesNotContain("sucursalSugeridaId", f.ParaRevisar);
        _facturas.Verify(x => x.ObtenerSucursalIdPorCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SinNingunaSucursalEnElDestinatario_NoSugiereNada()
    {
        var f = await Interpretar(PdfSintetico.PresupuestoFazio());

        Assert.Null(f.SucursalSugeridaId);
        Assert.DoesNotContain("sucursalSugeridaId", f.ParaRevisar);
    }

    [Fact]
    public async Task SiLaSucursalNoExisteEnLaBase_NoSugiereNada()
    {
        _facturas.Setup(x => x.ObtenerSucursalIdPorCodigoAsync("SV", It.IsAny<CancellationToken>())).ReturnsAsync((short?)null);

        var f = await Interpretar(PdfSintetico.FacturaExpoyer(domicilio: "SAN VICENTE 100"));

        Assert.Null(f.SucursalSugeridaId);
        Assert.DoesNotContain("sucursalSugeridaId", f.ParaRevisar);
    }

    [Theory]
    [InlineData("Mariano Fragueiro 3401", "FR")]
    [InlineData("MARIANO FRAGUEIRO 3401 LOCAL 2 / CORDOBA", "FR")]
    [InlineData("ex multirep fragueiro", "FR")]
    [InlineData("Multirep Fragueiro", "FR")]
    [InlineData("fragueiro", "FR")]
    [InlineData("San Vicente 1234", "SV")]
    [InlineData("SAN  VICENTE", "SV")]
    [InlineData("SAN JERONIMO 3029 - CP: 5000", "SV")]
    [InlineData("San Jerónimo 3029", "SV")]
    [InlineData("SAN JERÓNIMO", "SV")]
    [InlineData("san jeronimo", "SV")]
    [InlineData("Fragueiro y San Vicente", null)]
    [InlineData("FRAGUEIRO / SAN JERONIMO", null)]
    [InlineData("Calle Falsa 123", null)]
    [InlineData("San José 100", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ElReconocedorDeSucursal(string? texto, string? esperado)
        => Assert.Equal(esperado, SucursalDestinataria.Reconocer(texto));
}

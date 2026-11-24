using LaCentral.Api.Pdf;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// Fix E2 · orden de resolución del proveedor sin CUIT: la razón social EXACTA primero (una asigna, dos o más
/// no asignan y no caen a «contiene») y, sin exactas, «contiene» como hoy. Lógica del caso de uso con Moq; la
/// consulta de exactas se prueba contra PostgreSQL en <see cref="ProveedorRazonSocialExactaPostgreSqlTests"/>.
/// </summary>
public class ProveedorRazonSocialExactaTests
{
    private readonly Mock<ILectorTextoPdf> _lector = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IFacturaCompraRepositorio> _facturas = new();

    public ProveedorRazonSocialExactaTests()
    {
        _proveedores.Setup(p => p.BuscarActivosPorRazonSocialExactaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _proveedores.Setup(p => p.BuscarActivosPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _facturas.Setup(f => f.ObtenerArticulosPorCodigoProveedorAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
    }

    private void LasExactasSon(params Proveedor[] proveedores)
        => _proveedores.Setup(p => p.BuscarActivosPorRazonSocialExactaAsync("Fazio", It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(proveedores);

    private void ElContieneDa(params Proveedor[] proveedores)
        => _proveedores.Setup(p => p.BuscarActivosPorRazonSocialAsync("Fazio", It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(proveedores);

    private async Task<FacturaPdfInterpretada> Interpretar(int? proveedorElegido = null)
    {
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(new LectorTextoPdf().LeerRenglones(PdfSintetico.PresupuestoFazio()));
        var r = await new InterpretarFacturaPdfUseCase(_lector.Object, _proveedores.Object, _facturas.Object)
            .EjecutarAsync([1], null, proveedorElegido);
        Assert.True(r.IsSuccess, r.Error);
        return r.Value;
    }

    private void ElContieneNoSeConsulto()
        => _proveedores.Verify(p => p.BuscarActivosPorRazonSocialAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    // CA-1 / CA-6
    [Fact]
    public async Task UnaExacta_SeAsigna_AunqueElContieneDaVarias_YQuedaParaRevisar()
    {
        LasExactasSon(new Proveedor { Id = 47, Activo = true });
        ElContieneDa(new Proveedor { Id = 47, Activo = true }, new Proveedor { Id = 86, Activo = true });

        var f = await Interpretar();

        Assert.Equal(47, f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
        ElContieneNoSeConsulto();
        _facturas.Verify(x => x.ObtenerArticulosPorCodigoProveedorAsync(47, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-2
    [Fact]
    public async Task DosOMasExactas_NoAsignanNada_YNoCaenAContiene()
    {
        LasExactasSon(new Proveedor { Id = 47, Activo = true }, new Proveedor { Id = 48, Activo = true });
        ElContieneDa(new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar();

        Assert.Null(f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
        ElContieneNoSeConsulto();
        _facturas.Verify(x => x.ObtenerArticulosPorCodigoProveedorAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LaBusquedaExacta_PideAlMenosDos_ParaPoderVerLaAmbiguedad()
    {
        LasExactasSon(new Proveedor { Id = 47, Activo = true });

        await Interpretar();

        _proveedores.Verify(p => p.BuscarActivosPorRazonSocialExactaAsync("Fazio", It.Is<int>(limite => limite >= 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-3
    [Fact]
    public async Task SinExactas_ElContieneSeAplicaComoHoy_UnaCoincidenciaAsigna()
    {
        ElContieneDa(new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar();

        Assert.Equal(9, f.ProveedorId);
        Assert.Contains("proveedorId", f.ParaRevisar);
    }

    [Fact]
    public async Task SinExactas_ElContieneSeAplicaComoHoy_DosCoincidenciasNoAsignan()
    {
        ElContieneDa(new Proveedor { Id = 9, Activo = true }, new Proveedor { Id = 10, Activo = true });

        var f = await Interpretar();

        Assert.Null(f.ProveedorId);
        _proveedores.Verify(p => p.BuscarActivosPorRazonSocialAsync("Fazio", It.Is<int>(limite => limite >= 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-4 (la consulta solo trae activos; acá, que el caso de uso tampoco cuenta uno inactivo)
    [Fact]
    public async Task UnaExactaInactiva_NoCuentaComoExacta()
    {
        LasExactasSon(new Proveedor { Id = 47, Activo = false });
        ElContieneDa(new Proveedor { Id = 9, Activo = true });

        var f = await Interpretar();

        Assert.Equal(9, f.ProveedorId);
    }

    // CA-5
    [Fact]
    public async Task ElCuitManda_SobreLaExacta()
    {
        _proveedores.Setup(p => p.ObtenerPorCuitAsync(PdfSintetico.CuitProveedor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 7, Activo = true });
        _lector.Setup(l => l.LeerRenglones(It.IsAny<byte[]>())).Returns(new LectorTextoPdf().LeerRenglones(PdfSintetico.FacturaEtman()));
        _proveedores.Setup(p => p.BuscarActivosPorRazonSocialExactaAsync("ETMAN", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Proveedor { Id = 8, Activo = true }]);

        var r = await new InterpretarFacturaPdfUseCase(_lector.Object, _proveedores.Object, _facturas.Object).EjecutarAsync([1]);

        Assert.Equal(7, r.Value.ProveedorId);
        _proveedores.Verify(p => p.BuscarActivosPorRazonSocialExactaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ElProveedorElegidoAMano_MandaSobreLaExacta()
    {
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(new Proveedor { Id = 5, Activo = true });
        LasExactasSon(new Proveedor { Id = 47, Activo = true });

        var f = await Interpretar(proveedorElegido: 5);

        Assert.Equal(5, f.ProveedorId);
        _proveedores.Verify(p => p.BuscarActivosPorRazonSocialExactaAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

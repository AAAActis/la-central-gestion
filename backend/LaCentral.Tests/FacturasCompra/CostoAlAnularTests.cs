using LaCentral.UseCases.FacturasCompra;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// 028 CA-002 (v1.2) y 016 CA-004 (v1.1) · D-S7-03. La regla, sin base. La escritura en la base es del
/// repositorio y se prueba en AnularFacturaCompraPostgreSqlTests cuando el 13 esté aplicado y re-scaffoldeado.
/// </summary>
public class CostoAlAnularTests
{
    private static readonly RegistroCompraValido Anterior = new(80m, 3);

    [Fact]
    public void CA002_SiNadieTocoElCosto_VuelveAlDeAntesDeLaFactura()
    {
        var efecto = CostoAlAnular.Decidir(new(CostoActual: 100m, CostoPuestoPorLaFactura: 100m,
            CostoPrevioGuardado: true, CostoPrevio: 90m, AnteriorValido: Anterior));
        // 90, el guardado en la línea, aunque el último registro válido diga 80.
        Assert.Equal(new EfectoEnArticulo(true, 90m, 3), efecto);
    }

    [Fact]
    public void CA002_SiAntesNoTeniaCosto_VuelveANoTenerlo()
    {
        var efecto = CostoAlAnular.Decidir(new(100m, 100m, true, null, AnteriorValido: null));
        Assert.Equal(new EfectoEnArticulo(true, null, null), efecto);
    }

    [Fact]
    public void CA002_SiDespuesSeCambioAMano_QuedaElActual()
    {
        var efecto = CostoAlAnular.Decidir(new(CostoActual: 130m, CostoPuestoPorLaFactura: 100m,
            CostoPrevioGuardado: true, CostoPrevio: 90m, AnteriorValido: Anterior));
        Assert.Equal(new EfectoEnArticulo(false, 130m, 3), efecto);
    }

    [Fact]
    public void CA002_SiLoBorraronAMano_QuedaSinCosto()
    {
        // Costo desconocido es null: borrarlo a mano también es «cambiarlo a mano».
        var efecto = CostoAlAnular.Decidir(new(null, 100m, true, 90m, Anterior));
        Assert.Equal(new EfectoEnArticulo(false, null, 3), efecto);
    }

    [Fact]
    public void CA002_SinRegistroAnteriorValido_QuedaSinUltimoProveedor()
    {
        var efecto = CostoAlAnular.Decidir(new(100m, 100m, true, 90m, AnteriorValido: null));
        Assert.Equal(new EfectoEnArticulo(true, 90m, null), efecto);
    }

    [Fact]
    public void LineaAnteriorAl13_SigueComoV11_VuelveAlUltimoRegistroValido()
    {
        var efecto = CostoAlAnular.Decidir(new(130m, 100m, CostoPrevioGuardado: false, CostoPrevio: null, Anterior));
        Assert.Equal(new EfectoEnArticulo(true, 80m, 3), efecto);
    }

    [Fact]
    public void LineaAnteriorAl13_SinRegistroValido_ConservaElCostoYQuedaSinUltimoProveedor()
    {
        var efecto = CostoAlAnular.Decidir(new(130m, 100m, false, null, AnteriorValido: null));
        Assert.Equal(new EfectoEnArticulo(false, 130m, null), efecto);
    }
}

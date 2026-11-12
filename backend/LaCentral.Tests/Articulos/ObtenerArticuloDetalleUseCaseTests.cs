using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-02 · la ficha devuelve SIEMPRE las seis modalidades, cada una con precio o con
/// la razón de su ausencia. Nunca un precio en cero por defecto.
/// </summary>
public class ObtenerArticuloDetalleUseCaseTests
{
    private const int Id = 7;
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();

    private ObtenerArticuloDetalleUseCase Caso() => new(_consulta.Object);

    private void Detalle(decimal? costo, params PrecioModalidadConsultaDto[] precios) =>
        _consulta.Setup(c => c.ObtenerDetallePorIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloDetalleConsultaDto(Id, "LC-000007", "Bujía", true, costo, 3,
                new[] { new CodigoProveedorConsultaDto(3, "Norte", " bpr6es ", "BPR6ES") },
                new[] { new StockSucursalConsultaDto(1, "FR", 2.5m) },
                precios));

    // Las seis filas con que nace un artículo (ReglasPrecioModalidad.ConfiguracionInicial).
    private static PrecioModalidadConsultaDto[] ConfiguracionInicial() =>
        ReglasPrecioModalidad.ConfiguracionInicial()
            .Select(p => new PrecioModalidadConsultaDto(p.Modalidad, p.Base, p.Porcentaje, p.Precio,
                p.CalculoManual, p.FechaCalculo, p.UsuarioId))
            .ToArray();

    private static PrecioModalidadFichaDto Fila(Result<FichaArticuloDto> r, Modalidad m)
    {
        Assert.True(r.IsSuccess);
        return Assert.Single(r.Value!.Precios, p => p.Modalidad == m);
    }

    [Fact]
    public async Task Ficha_ArticuloInexistente_NoEncontrado()
    {
        var resultado = await Caso().EjecutarAsync(Id);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    [Fact]
    public async Task Ficha_CopiaLosDatosDelArticulo()
    {
        Detalle(costo: 100m, ConfiguracionInicial());

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess);
        var ficha = resultado.Value!;
        Assert.Equal((Id, "LC-000007", "Bujía", true, 100m, (int?)3), (ficha.Id, ficha.CodigoInterno, ficha.Nombre, ficha.Activo, ficha.CostoVigente, ficha.UltimoProveedorId));
        Assert.Equal("BPR6ES", Assert.Single(ficha.Codigos).ClaveNormalizada);
        Assert.Equal(2.5m, Assert.Single(ficha.Existencias).Cantidad);
    }

    // HU-STK-01 CA-001 y CA-003: el total suma cada sucursal, y un negativo resta, no se ignora.
    [Fact]
    public async Task Ficha_TotalExistencias_SumaLasSucursalesConSuSigno()
    {
        _consulta.Setup(c => c.ObtenerDetallePorIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloDetalleConsultaDto(Id, "LC-000007", "Bujía", true, null, null,
                Array.Empty<CodigoProveedorConsultaDto>(),
                new[] { new StockSucursalConsultaDto(1, "FR", 5m), new StockSucursalConsultaDto(2, "SV", -1.5m) },
                Array.Empty<PrecioModalidadConsultaDto>()));

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(3.5m, resultado.Value!.TotalExistencias);
    }

    [Fact]
    public async Task Ficha_SinFilas_SeisModalidadesSinConfiguracionEnElOrdenDelEnum()
    {
        Detalle(costo: 100m);

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(Enum.GetValues<Modalidad>(), resultado.Value!.Precios.Select(p => p.Modalidad));
        Assert.All(resultado.Value.Precios, p =>
        {
            Assert.Null(p.Precio);
            Assert.Equal(RazonAusenciaPrecio.SinConfiguracion, p.RazonAusencia);
        });
    }

    [Fact]
    public async Task Ficha_ArticuloRecienCreadoSinCosto_CadaModalidadConSuRazon()
    {
        Detalle(costo: null, ConfiguracionInicial());

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(
            new RazonAusenciaPrecio?[]
            {
                RazonAusenciaPrecio.SinCosto,             // Lista
                RazonAusenciaPrecio.TasaSinDefinir,       // Tarjeta3
                RazonAusenciaPrecio.TasaSinDefinir,       // Tarjeta6
                RazonAusenciaPrecio.SinCosto,             // EfectivoTransferencia
                RazonAusenciaPrecio.SinCosto,             // Costo
                RazonAusenciaPrecio.CasoEspecialNoFijado  // CasoEspecial
            },
            resultado.Value!.Precios.Select(p => p.RazonAusencia));
        Assert.All(resultado.Value.Precios, p => Assert.Null(p.Precio));
    }

    [Fact]
    public async Task Ficha_FaltaUnaFila_EsaModalidadSinConfiguracionYLasDemasConLaSuya()
    {
        Detalle(costo: null, ConfiguracionInicial().Where(p => p.Modalidad != Modalidad.Tarjeta6).ToArray());

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(6, resultado.Value!.Precios.Count);
        Assert.Equal(RazonAusenciaPrecio.SinConfiguracion, Fila(resultado, Modalidad.Tarjeta6).RazonAusencia);
        Assert.Equal(RazonAusenciaPrecio.TasaSinDefinir, Fila(resultado, Modalidad.Tarjeta3).RazonAusencia);
    }

    [Fact]
    public async Task Ficha_ConPrecio_SinRazonYConTodosLosDatosDeLaFila()
    {
        var fecha = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
        Detalle(costo: 100m,
            new PrecioModalidadConsultaDto(Modalidad.Lista, BasePrecio.CostoArticulo, 50m, 150m, false, fecha, 9),
            new PrecioModalidadConsultaDto(Modalidad.CasoEspecial, null, null, 120m, true, fecha, 9));

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.Equal(new PrecioModalidadFichaDto(Modalidad.Lista, BasePrecio.CostoArticulo, 50m, 150m, false, fecha, 9, null),
            Fila(resultado, Modalidad.Lista));
        Assert.Equal(new PrecioModalidadFichaDto(Modalidad.CasoEspecial, null, null, 120m, true, fecha, 9, null),
            Fila(resultado, Modalidad.CasoEspecial));
    }

    // Manual sin precio es "caso especial no fijado" aunque tampoco haya costo: el costo
    // no lo arreglaría, lo fija una persona.
    [Fact]
    public async Task Ficha_ManualSinPrecioYSinCosto_CasoEspecialNoFijado()
    {
        Detalle(costo: null, new PrecioModalidadConsultaDto(Modalidad.Lista, null, null, null, true, null, null));

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.Equal(RazonAusenciaPrecio.CasoEspecialNoFijado, Fila(resultado, Modalidad.Lista).RazonAusencia);
    }

    // Sin base es "tasa sin definir" aunque tampoco haya costo: con costo seguiría sin precio.
    [Fact]
    public async Task Ficha_SinBaseYSinCosto_TasaSinDefinir()
    {
        Detalle(costo: null, new PrecioModalidadConsultaDto(Modalidad.Tarjeta3, null, null, null, false, null, null));

        var resultado = await Caso().EjecutarAsync(Id);

        Assert.Equal(RazonAusenciaPrecio.TasaSinDefinir, Fila(resultado, Modalidad.Tarjeta3).RazonAusencia);
    }
}

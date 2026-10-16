using LaCentral.Api.Middleware;
using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-01 contra PostgreSQL real, con el caso de uso, los repositorios y la unidad de
/// trabajo de producción. Todo se relee con un contexto nuevo.
///
/// La atomicidad es el test más importante del sprint: por cada punto de escritura hay
/// un test que hace fallar AL MOTOR ahí (un trigger en el esquema aislado que lanza una
/// excepción) y afirma que no quedó cabecera, detalle, stock, costo, último proveedor ni
/// historial. En detalle, stock, artículo e historial el trigger salta en la SEGUNDA
/// línea, cuando la primera ya escribió. La cabecera es una sola fila: su falla (trigger
/// o índice de número activo) ocurre antes de cualquier escritura, y esos tests cuidan que
/// nada se escriba antes que ella. Los de stock, artículo e historial son los que dependen
/// de la transacción de IUnidadDeTrabajo: sin ella quedan en rojo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class RegistrarFacturaCompraPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public RegistrarFacturaCompraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private sealed record Escenario(
        PostgreSqlPruebas Fixture, int UsuarioId, int Proveedor, int ProveedorAnterior,
        int Comprado1, int Comprado2, int NoComprado, short Sucursal1, short Sucursal2);

    private static readonly DateTime CalculoAnterior = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    /// <summary>Las seis filas del alta, calculadas el 1/9 con ese costo. Con usuario, CasoEspecial queda fijado a mano en 80.</summary>
    private static List<ArticuloPrecioModalidad> Precios(decimal? costo, int? usuarioCasoEspecial = null) =>
        CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), costo, CalculoAnterior)
            .Select(p => new ArticuloPrecioModalidad
            {
                Modalidad = p.Modalidad.ToString(), Base = p.Base?.ToString(), Porcentaje = p.Porcentaje,
                Precio = p.Modalidad == LaCentral.UseCases.Entidades.Modalidad.CasoEspecial && usuarioCasoEspecial is not null ? 80m : p.Precio,
                CalculoManual = p.CalculoManual,
                FechaCalculo = p.Modalidad == LaCentral.UseCases.Entidades.Modalidad.CasoEspecial && usuarioCasoEspecial is not null ? CalculoAnterior : p.FechaCalculo,
                UsuarioId = p.Modalidad == LaCentral.UseCases.Entidades.Modalidad.CasoEspecial ? usuarioCasoEspecial : null
            }).ToList();

    /// <summary>
    /// Comprado1 tiene costo y último proveedor previos y stock en las dos sucursales.
    /// Comprado2 no tiene costo y solo tiene fila de stock en la sucursal 1: la compra
    /// lo manda a la 2. NoComprado no aparece en la factura y no puede cambiar.
    /// Los tres tienen las seis filas de precio del alta. Comprado1 tiene además su
    /// CasoEspecial fijado a mano (80, por el usuario); NoComprado tiene los precios de su
    /// costo 70 calculados el 1/9.
    /// </summary>
    private static async Task<Escenario> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuarioId = await fixture.CrearUsuarioAsync();
        await using var contexto = fixture.CrearContexto();
        var sucursales = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).Take(2).ToArrayAsync();
        var proveedor = new Proveedor { Codigo = "PRV-FCO", RazonSocial = "Proveedor FCO", Activo = true };
        var anterior = new Proveedor { Codigo = "PRV-ANT", RazonSocial = "Proveedor anterior", Activo = true };
        contexto.Proveedor.AddRange(proveedor, anterior);
        await contexto.SaveChangesAsync();

        Articulo Articulo(string codigo, decimal? costo, int? ultimoProveedor, params (short Sucursal, decimal Cantidad)[] stock) => new()
        {
            CodigoInterno = codigo, Nombre = $"Artículo {codigo}", Activo = true,
            PrecioCosto = costo, UltimoProveedorId = ultimoProveedor,
            Stock = stock.Select(s => new Stock { SucursalId = s.Sucursal, Cantidad = s.Cantidad }).ToList()
        };
        var comprado1 = Articulo("FCO-1", 50m, anterior.Id, (sucursales[0], 3m), (sucursales[1], 0m));
        comprado1.ArticuloPrecioModalidad = Precios(50m, usuarioCasoEspecial: usuarioId);
        var comprado2 = Articulo("FCO-2", null, null, (sucursales[0], 0m));
        comprado2.ArticuloPrecioModalidad = Precios(null);
        var noComprado = Articulo("FCO-3", 70m, anterior.Id, (sucursales[0], 4m), (sucursales[1], 4m));
        noComprado.ArticuloPrecioModalidad = Precios(70m);
        contexto.Articulo.AddRange(comprado1, comprado2, noComprado);
        await contexto.SaveChangesAsync();

        return new(fixture, usuarioId, proveedor.Id, anterior.Id, comprado1.Id, comprado2.Id, noComprado.Id,
            sucursales[0], sucursales[1]);
    }

    /// <summary>La sesión está en la sucursal 1: el stock va al destino de cada línea (D9), no a la sesión.</summary>
    private static RegistrarFacturaCompraUseCase Caso(LaCentralDbContext contexto, Escenario e,
        CalcularPreciosArticuloUseCase? recalculo = null)
    {
        var sesion = new Mock<IContextoUsuario>();
        sesion.SetupGet(s => s.UsuarioId).Returns(e.UsuarioId);
        sesion.SetupGet(s => s.SucursalId).Returns(e.Sucursal1);
        return new(new FacturaCompraRepositorio(contexto), new ProveedorRepositorio(contexto),
            new ArticuloRepositorio(contexto), new UnidadDeTrabajo(contexto), sesion.Object,
            recalculo ?? new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(contexto)));
    }

    private static RegistrarFacturaCompraRequest Compra(Escenario e, string numero = "A-0001-00000001") => new(
        e.Proveedor, numero, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), 30m, 4.5m, 1.25m, 0.75m,
        new[]
        {
            new LineaFacturaCompraRequest(e.Comprado1, e.Sucursal1, 5m, 200m, 10m),
            new LineaFacturaCompraRequest(e.Comprado2, e.Sucursal2, 2.5m, 80m, 0m)
        });

    // ---- CA1, CA4 y D9 ----

    [PostgreSqlFact]
    public async Task Registrar_EscribeFacturaStockCostoUltimoProveedorEHistorial_SoloDeLosComprados()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        Result<RegistrarFacturaCompraResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Caso(contexto, e).EjecutarAsync(Compra(e));

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();

        var factura = await relectura.FacturaCompra.AsNoTracking().SingleAsync();
        Assert.Equal(resultado.Value.Id, factura.Id);
        Assert.Equal(e.Proveedor, factura.ProveedorId);
        Assert.Equal("A-0001-00000001", factura.Numero);
        Assert.Equal(new DateOnly(2026, 10, 1), factura.FechaEmision);
        Assert.Equal(new DateOnly(2026, 10, 31), factura.FechaVencimiento);
        Assert.Equal((30m, 4.5m, 1.25m, 0.75m),
            (factura.PercepcionIva, factura.PercepcionIibb, factura.PercepcionMunicipal, factura.PercepcionGanancias));
        Assert.Equal(("ACTIVA", "MANUAL"), (factura.Estado, factura.Origen));
        Assert.Equal(e.UsuarioId, factura.UsuarioId);

        var lineas = await relectura.FacturaCompraDetalle.AsNoTracking().OrderBy(d => d.ArticuloId).ToListAsync();
        Assert.Collection(lineas,
            l => Assert.Equal((factura.Id, e.Comprado1, e.Sucursal1, 5m, 200m, 10m),
                (l.FacturaCompraId, l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje)),
            l => Assert.Equal((factura.Id, e.Comprado2, e.Sucursal2, 2.5m, 80m, 0m),
                (l.FacturaCompraId, l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje)));

        // D9: cada línea suma en SU destino. Comprado2 no tenía fila en la sucursal 2: se crea.
        var stock = await StockAsync(relectura);
        Assert.Equal(8m, stock[(e.Comprado1, e.Sucursal1)]);
        Assert.Equal(0m, stock[(e.Comprado1, e.Sucursal2)]);
        Assert.Equal(0m, stock[(e.Comprado2, e.Sucursal1)]);
        Assert.Equal(2.5m, stock[(e.Comprado2, e.Sucursal2)]);
        Assert.Equal(4m, stock[(e.NoComprado, e.Sucursal1)]);
        Assert.Equal(4m, stock[(e.NoComprado, e.Sucursal2)]);

        // Costo = precio neto del descuento. El no comprado conserva todo.
        var articulos = await ArticulosAsync(relectura);
        Assert.Equal((180m, (int?)e.Proveedor), articulos[e.Comprado1]);
        Assert.Equal((80m, (int?)e.Proveedor), articulos[e.Comprado2]);
        Assert.Equal((70m, (int?)e.ProveedorAnterior), articulos[e.NoComprado]);

        var historial = await relectura.ArticuloHistorialCompra.AsNoTracking().OrderBy(h => h.ArticuloId).ToListAsync();
        Assert.Collection(historial,
            h => Assert.Equal((e.Comprado1, e.Proveedor, factura.Id, 180m, false, factura.FechaRegistro),
                (h.ArticuloId, h.ProveedorId, h.FacturaCompraId, h.PrecioCosto, h.Anulado, h.Fecha)),
            h => Assert.Equal((e.Comprado2, e.Proveedor, factura.Id, 80m, false, factura.FechaRegistro),
                (h.ArticuloId, h.ProveedorId, h.FacturaCompraId, h.PrecioCosto, h.Anulado, h.Fecha)));
    }

    // ---- CA5 (HU-ART-05 CA-001 y CA-002): recálculo de los comprados, y solo de ellos ----

    [PostgreSqlFact]
    public async Task Registrar_RecalculaLosPreciosDeLosComprados_YNoLosDelResto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Dictionary<(int Articulo, string Modalidad), (string? Base, decimal? Porcentaje, decimal? Precio, bool Manual, DateTime? Fecha, int? Usuario)> antes;
        await using (var lectura = fixture.CrearContexto())
            antes = await PreciosAsync(lectura);

        Result<RegistrarFacturaCompraResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Caso(contexto, e).EjecutarAsync(Compra(e));

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        var registro = (await relectura.FacturaCompra.AsNoTracking().SingleAsync()).FechaRegistro;
        var precios = await PreciosAsync(relectura);

        // Comprado1: costo 180 → Lista 270, Costo 180, Efectivo 243, con la fecha del registro.
        Assert.Equal(("CostoArticulo", (decimal?)50m, (decimal?)270m, false, (DateTime?)registro, (int?)null), precios[(e.Comprado1, "Lista")]);
        Assert.Equal(("CostoArticulo", (decimal?)0m, (decimal?)180m, false, (DateTime?)registro, (int?)null), precios[(e.Comprado1, "Costo")]);
        Assert.Equal(("PrecioLista", (decimal?)-10m, (decimal?)243m, false, (DateTime?)registro, (int?)null), precios[(e.Comprado1, "EfectivoTransferencia")]);
        Assert.Equal(((string?)null, (decimal?)null, (decimal?)null, false, (DateTime?)null, (int?)null), precios[(e.Comprado1, "Tarjeta3")]);
        Assert.Equal(((string?)null, (decimal?)null, (decimal?)null, false, (DateTime?)null, (int?)null), precios[(e.Comprado1, "Tarjeta6")]);
        // El CasoEspecial fijado a mano no se pisa.
        Assert.Equal(antes[(e.Comprado1, "CasoEspecial")], precios[(e.Comprado1, "CasoEspecial")]);
        Assert.Equal((decimal?)80m, precios[(e.Comprado1, "CasoEspecial")].Precio);

        // Comprado2 no tenía costo: ahora 80 → Lista 120, Costo 80, Efectivo 108.
        Assert.Equal(((decimal?)120m, (decimal?)80m, (decimal?)108m),
            (precios[(e.Comprado2, "Lista")].Precio, precios[(e.Comprado2, "Costo")].Precio, precios[(e.Comprado2, "EfectivoTransferencia")].Precio));
        Assert.Equal((DateTime?)registro, precios[(e.Comprado2, "Lista")].Fecha);

        // El no comprado conserva sus seis filas tal cual (CA-002 de HU-ART-05).
        Assert.All(antes.Where(p => p.Key.Articulo == e.NoComprado), p => Assert.Equal(p.Value, precios[p.Key]));
        Assert.Equal((decimal?)105m, precios[(e.NoComprado, "Lista")].Precio);
    }

    // O1 de la Fase 6: el recálculo lee las filas, otra persona fija a mano el CasoEspecial
    // del mismo artículo y confirma, y recién después el recálculo escribe. La fila manual
    // tiene que quedar con lo que fijó esa persona, no con lo que el recálculo leyó antes.
    [PostgreSqlFact]
    public async Task Registrar_PrecioManualFijadoMientrasSeRecalcula_SobreviveALaCompra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var fijado = new DateTime(2026, 10, 2, 9, 30, 0, DateTimeKind.Utc);

        Result<RegistrarFacturaCompraResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
        {
            var precios = new PreciosConFijacionConcurrente(new ArticuloRepositorio(contexto), async () =>
            {
                await using var otraSesion = fixture.CrearContexto();
                await otraSesion.ArticuloPrecioModalidad
                    .Where(p => p.ArticuloId == e.Comprado1 && p.Modalidad == "CasoEspecial")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Precio, 95m)
                        .SetProperty(p => p.FechaCalculo, fijado));
            });
            resultado = await Caso(contexto, e, new CalcularPreciosArticuloUseCase(precios)).EjecutarAsync(Compra(e));
        }

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        var casoEspecial = (await PreciosAsync(relectura))[(e.Comprado1, "CasoEspecial")];
        Assert.Equal(((string?)null, (decimal?)null, (decimal?)95m, true, (DateTime?)fijado, (int?)e.UsuarioId), casoEspecial);
    }

    /// <summary>El repositorio de producción, con la fijación de otra sesión entre la lectura y la escritura.</summary>
    private sealed class PreciosConFijacionConcurrente : IPreciosArticuloRepositorio
    {
        private readonly IPreciosArticuloRepositorio _real;
        private readonly Func<Task> _fijar;

        public PreciosConFijacionConcurrente(IPreciosArticuloRepositorio real, Func<Task> fijar) => (_real, _fijar) = (real, fijar);

        public async Task<IReadOnlyDictionary<int, PreciosDeArticulo>> ObtenerAsync(IReadOnlyCollection<int> articuloIds, CancellationToken ct = default)
        {
            var leidos = await _real.ObtenerAsync(articuloIds, ct);
            await _fijar();
            return leidos;
        }

        public Task ActualizarPreciosAsync(int articuloId, IReadOnlyList<LaCentral.UseCases.Entidades.PrecioModalidad> precios, CancellationToken ct = default) =>
            _real.ActualizarPreciosAsync(articuloId, precios, ct);
    }

    // Invariante de SinCosto (Fase 4): después de una compra, ningún comprado queda con una
    // modalidad con base y sin precio.
    [PostgreSqlFact]
    public async Task Registrar_NingunCompradoQuedaConBaseYSinPrecio()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Caso(contexto, e).EjecutarAsync(Compra(e))).IsSuccess);

        await using var relectura = fixture.CrearContexto();
        var conBaseSinPrecio = await relectura.ArticuloPrecioModalidad.AsNoTracking()
            .Where(p => (p.ArticuloId == e.Comprado1 || p.ArticuloId == e.Comprado2) && p.Base != null && p.Precio == null)
            .Select(p => new { p.ArticuloId, p.Modalidad }).ToListAsync();
        Assert.Empty(conBaseSinPrecio);
    }

    // Un costo que entra en su columna pero da una Lista que no entra en NUMERIC(12,2):
    // Invalido, sin excepción, y no queda nada de la compra.
    [PostgreSqlFact]
    public async Task PrecioRecalculadoQueNoEntraEnSuColumna_InvalidoYNoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var antes = await FotografiarAsync(fixture);
        var compra = Compra(e) with
        {
            Lineas = new[]
            {
                new LineaFacturaCompraRequest(e.Comprado2, e.Sucursal2, 1m, 80m, 0m),
                new LineaFacturaCompraRequest(e.Comprado1, e.Sucursal1, 1m, 6_666_666_666.67m, 0m)
            }
        };

        Exception? error = null;
        Result<RegistrarFacturaCompraResponse>? resultado = null;
        await using (var contexto = fixture.CrearContexto())
        {
            try { resultado = await Caso(contexto, e).EjecutarAsync(compra); }
            catch (Exception excepcion) { error = excepcion; }
        }

        Assert.Null(error);
        Assert.Equal(TipoError.Invalido, resultado!.Tipo);
        Assert.Contains($"artículo {e.Comprado1}", resultado.Error);
        Assert.Equal(antes, await FotografiarAsync(fixture));
    }

    [PostgreSqlFact]
    public async Task PreciosRepositorio_ObtenerDevuelveCostoYFilasDeLosPedidos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var sinFilas = fixture.CrearContexto())
            await sinFilas.ArticuloPrecioModalidad.Where(p => p.ArticuloId == e.Comprado2).ExecuteDeleteAsync();

        await using var contexto = fixture.CrearContexto();
        var leidos = await new ArticuloRepositorio(contexto).ObtenerAsync(new[] { e.Comprado1, e.Comprado2, int.MaxValue });

        Assert.Equal(new[] { e.Comprado1, e.Comprado2 }, leidos.Keys.Order());
        Assert.Equal(50m, leidos[e.Comprado1].CostoVigente);
        Assert.Empty(leidos[e.Comprado2].Precios);
        var esperadas = CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), 50m, CalculoAnterior)
            .Select(p => p.Modalidad == LaCentral.UseCases.Entidades.Modalidad.CasoEspecial
                ? p with { Precio = 80m, FechaCalculo = CalculoAnterior, UsuarioId = e.UsuarioId }
                : p)
            .OrderBy(p => p.Modalidad);
        Assert.Equal(esperadas, leidos[e.Comprado1].Precios.OrderBy(p => p.Modalidad));
    }

    // ---- CA2 ----

    [PostgreSqlFact]
    public async Task ObtenerNumeroActivo_SoloLaActivaDelMismoProveedor()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var activa = await SembrarFacturaAsync(e, e.Proveedor, "A-1", anulada: false);
        await SembrarFacturaAsync(e, e.Proveedor, "A-ANULADA", anulada: true);

        await using var contexto = fixture.CrearContexto();
        var repositorio = new FacturaCompraRepositorio(contexto);

        Assert.Equal(new FacturaCompraExistente(activa, "A-1", new DateOnly(2026, 9, 15)),
            await repositorio.ObtenerNumeroActivoAsync(e.Proveedor, "A-1"));
        Assert.Null(await repositorio.ObtenerNumeroActivoAsync(e.Proveedor, "A-ANULADA"));
        Assert.Null(await repositorio.ObtenerNumeroActivoAsync(e.ProveedorAnterior, "A-1"));
    }

    [PostgreSqlFact]
    public async Task NumeroActivoRepetido_ConflictoYNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var activa = await SembrarFacturaAsync(e, e.Proveedor, "A-0001-00000001", anulada: false);
        var antes = await FotografiarAsync(fixture);

        Result<RegistrarFacturaCompraResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Caso(contexto, e).EjecutarAsync(Compra(e));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains($"Id {activa}", resultado.Error);
        Assert.Equal(antes, await FotografiarAsync(fixture));
    }

    [PostgreSqlFact]
    public async Task ObtenerEstadoArticulos_ActivosEInactivosYSinLosInexistentes()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var baja = fixture.CrearContexto())
            await baja.Articulo.Where(a => a.Id == e.NoComprado).ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Activo, false).SetProperty(a => a.MotivoBaja, "Discontinuado")
                .SetProperty(a => a.FechaBaja, DateTime.UtcNow));

        await using var contexto = fixture.CrearContexto();
        var estado = await new FacturaCompraRepositorio(contexto)
            .ObtenerEstadoArticulosAsync(new[] { e.Comprado1, e.NoComprado, int.MaxValue });

        Assert.Equal(new Dictionary<int, bool> { [e.Comprado1] = true, [e.NoComprado] = false }, estado);
    }

    // ---- Atomicidad: un test por cada punto de falla ----

    /// <summary>
    /// Punto de falla real de la cabecera: dos registros simultáneos con el mismo número
    /// pasan la validación previa y el índice parcial rechaza el segundo.
    /// </summary>
    [PostgreSqlFact]
    public async Task Atomicidad_CabeceraRechazadaPorElIndiceDeNumeroActivo_NoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await SembrarFacturaAsync(e, e.Proveedor, "A-0001-00000001", anulada: false);
        var antes = await FotografiarAsync(fixture);

        await using (var contexto = fixture.CrearContexto())
        {
            // Sin pasar por la validación del núcleo: es la carrera que solo cierra el índice.
            var factura = new LaCentral.UseCases.Entidades.FacturaCompra
            {
                ProveedorId = e.Proveedor, Numero = "A-0001-00000001", FechaEmision = new DateOnly(2026, 10, 1),
                UsuarioId = e.UsuarioId, FechaRegistro = DateTime.UtcNow,
                Lineas = { new() { ArticuloId = e.Comprado1, SucursalId = e.Sucursal1, Cantidad = 1m, PrecioUnitario = 10m } }
            };
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => new UnidadDeTrabajo(contexto).EjecutarAsync(async ct =>
            {
                await new FacturaCompraRepositorio(contexto).RegistrarConEfectosAsync(factura, ct);
                return Result.Success();
            }));
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
            Assert.Equal("ux_factura_compra_numero_activa", postgres.ConstraintName);
            // El manejador global lo informa como 409, no como 500.
            Assert.NotNull(ManejadorExcepcionesGlobal.MensajeDeConflicto(error));
        }

        Assert.Equal(antes, await FotografiarAsync(fixture));
    }

    /// <summary>
    /// La cantidad entra en NUMERIC(12,3), pero sumada al stock existente no: el motor
    /// no puede guardarla (22003). Tiene que ser Invalido, no un 500, y la primera línea
    /// (que ya escribió) se descarta con todo lo demás.
    /// </summary>
    [PostgreSqlFact]
    public async Task StockQueDesbordaSuColumna_InvalidoYNoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var antes = await FotografiarAsync(fixture);
        var compra = Compra(e) with
        {
            Lineas = new[]
            {
                new LineaFacturaCompraRequest(e.Comprado2, e.Sucursal2, 2.5m, 80m, 0m),
                new LineaFacturaCompraRequest(e.Comprado1, e.Sucursal1, 999_999_999.999m, 1m, 0m)
            }
        };

        Exception? error = null;
        Result<RegistrarFacturaCompraResponse>? resultado = null;
        await using (var contexto = fixture.CrearContexto())
        {
            try { resultado = await Caso(contexto, e).EjecutarAsync(compra); }
            catch (Exception excepcion) { error = excepcion; }
        }

        Assert.Null(error);
        Assert.Equal(TipoError.Invalido, resultado!.Tipo);
        Assert.Contains($"{e.Comprado1}", resultado.Error);
        Assert.Equal(antes, await FotografiarAsync(fixture));
    }

    /// <summary>
    /// El borde exacto: 0,001 + 999.999.999,999 = 1.000.000.000, que ya no entra en
    /// NUMERIC(12,3). Fija el límite del WHERE del upsert: con <c>&lt;=</c> la suma pasaría
    /// y el motor daría 22003 en lugar de Invalido.
    /// </summary>
    [PostgreSqlFact]
    public async Task StockQueLlegaJustoAlLimiteDeSuColumna_InvalidoYNoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var contexto = fixture.CrearContexto())
            await contexto.Stock.Where(s => s.ArticuloId == e.Comprado1 && s.SucursalId == e.Sucursal1)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 0.001m));
        var antes = await FotografiarAsync(fixture);
        var compra = Compra(e) with
        {
            Lineas = new[] { new LineaFacturaCompraRequest(e.Comprado1, e.Sucursal1, 999_999_999.999m, 1m, 0m) }
        };

        Exception? error = null;
        Result<RegistrarFacturaCompraResponse>? resultado = null;
        await using (var contexto = fixture.CrearContexto())
        {
            try { resultado = await Caso(contexto, e).EjecutarAsync(compra); }
            catch (Exception excepcion) { error = excepcion; }
        }

        Assert.Null(error);
        Assert.Equal(TipoError.Invalido, resultado!.Tipo);
        Assert.Contains($"{e.Comprado1}", resultado.Error);
        Assert.Equal(antes, await FotografiarAsync(fixture));
    }

    /// <summary>
    /// El otro lado del borde: 0,001 + 999.999.999,998 = 999.999.999,999, el máximo que
    /// guarda NUMERIC(12,3). Tiene que entrar. Fija el valor de <c>LimiteStock</c>: si
    /// bajara una milésima, esta compra daría Invalido.
    /// </summary>
    [PostgreSqlFact]
    public async Task StockQueQuedaEnElMaximoDeSuColumna_SeRegistraYSeGuarda()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var contexto = fixture.CrearContexto())
            await contexto.Stock.Where(s => s.ArticuloId == e.Comprado1 && s.SucursalId == e.Sucursal1)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 0.001m));
        var compra = Compra(e) with
        {
            Lineas = new[] { new LineaFacturaCompraRequest(e.Comprado1, e.Sucursal1, 999_999_999.998m, 1m, 0m) }
        };

        Result<RegistrarFacturaCompraResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Caso(contexto, e).EjecutarAsync(compra);

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        Assert.Equal(999_999_999.999m, (await StockAsync(relectura))[(e.Comprado1, e.Sucursal1)]);
        Assert.Equal(resultado.Value.Id, (await relectura.FacturaCompra.AsNoTracking().SingleAsync()).Id);
    }

    [PostgreSqlFact]
    public Task Atomicidad_FallaAlInsertarLaCabecera_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("factura_compra", "INSERT", e => $"NEW.proveedor_id = {e.Proveedor}");

    [PostgreSqlFact]
    public Task Atomicidad_FallaAlInsertarElDetalleDeLaSegundaLinea_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("factura_compra_detalle", "INSERT", e => $"NEW.articulo_id = {e.Comprado2}");

    [PostgreSqlFact]
    public Task Atomicidad_FallaAlIncrementarElStockDeLaSegundaLinea_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("stock", "INSERT OR UPDATE", e => $"NEW.articulo_id = {e.Comprado2}");

    [PostgreSqlFact]
    public Task Atomicidad_FallaAlActualizarCostoYUltimoProveedorDeLaSegundaLinea_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("articulo", "UPDATE", e => $"NEW.id = {e.Comprado2}");

    [PostgreSqlFact]
    public Task Atomicidad_FallaAlAgregarElHistorialDeLaSegundaLinea_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("articulo_historial_compra", "INSERT", e => $"NEW.articulo_id = {e.Comprado2}");

    // CA5: el recálculo es el último paso. Falla en el precio del segundo comprado, cuando
    // los del primero ya se escribieron.
    [PostgreSqlFact]
    public Task Atomicidad_FallaAlRecalcularLosPreciosDelSegundoComprado_NoQuedaNada() =>
        AfirmarQueNoQuedaNadaAsync("articulo_precio_modalidad", "UPDATE", e => $"NEW.articulo_id = {e.Comprado2}");

    private async Task AfirmarQueNoQuedaNadaAsync(string tabla, string eventos, Func<Escenario, string> condicion)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await InstalarFallaAsync(fixture, tabla, eventos, condicion(e));
        var antes = await FotografiarAsync(fixture);

        await using (var contexto = fixture.CrearContexto())
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() => Caso(contexto, e).EjecutarAsync(Compra(e)));
            Assert.Contains($"falla forzada en {tabla}", error.ToString());
        }

        var despues = await FotografiarAsync(fixture);
        Assert.Equal(antes, despues);
        Assert.Equal((0, 0, 0), (despues.Facturas, despues.Lineas, despues.Historial));
    }

    /// <summary>El motor lanza la excepción: un trigger en el esquema aislado, que se borra con él.</summary>
    private static async Task InstalarFallaAsync(PostgreSqlPruebas fixture, string tabla, string eventos, string condicion)
    {
        // DDL: no admite parámetros. Tabla, eventos y condición son constantes del test.
        var ddl = $"""
            CREATE FUNCTION falla_forzada() RETURNS trigger LANGUAGE plpgsql AS $cuerpo$
            BEGIN
                IF {condicion} THEN RAISE EXCEPTION 'falla forzada en {tabla}'; END IF;
                RETURN NEW;
            END $cuerpo$;
            CREATE TRIGGER falla_forzada BEFORE {eventos} ON {tabla}
                FOR EACH ROW EXECUTE FUNCTION falla_forzada();
            """;
        await using var contexto = fixture.CrearContexto();
        await contexto.Database.ExecuteSqlRawAsync(ddl);
    }

    private static async Task<int> SembrarFacturaAsync(Escenario e, int proveedorId, string numero, bool anulada)
    {
        await using var contexto = e.Fixture.CrearContexto();
        var factura = new FacturaCompra
        {
            ProveedorId = proveedorId, Numero = numero, FechaEmision = new DateOnly(2026, 9, 15),
            Estado = anulada ? "ANULADA" : "ACTIVA", Origen = "MANUAL", UsuarioId = e.UsuarioId,
            FechaRegistro = DateTime.UtcNow,
            MotivoAnulacion = anulada ? "Cargada por error" : null,
            FechaAnulacion = anulada ? DateTime.UtcNow : null
        };
        contexto.FacturaCompra.Add(factura);
        await contexto.SaveChangesAsync();
        return factura.Id;
    }

    /// <summary>Todo lo que una compra puede tocar, releído con un contexto nuevo.</summary>
    private sealed record Foto(int Facturas, int Lineas, int Historial, string Stock, string Articulos, string Precios);

    private static async Task<Foto> FotografiarAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        var stock = await StockAsync(contexto);
        var articulos = await ArticulosAsync(contexto);
        var precios = await PreciosAsync(contexto);
        return new(
            await contexto.FacturaCompra.CountAsync(),
            await contexto.FacturaCompraDetalle.CountAsync(),
            await contexto.ArticuloHistorialCompra.CountAsync(),
            string.Join("; ", stock.OrderBy(s => s.Key).Select(s => $"{s.Key}={s.Value}")),
            string.Join("; ", articulos.OrderBy(a => a.Key).Select(a => $"{a.Key}={a.Value}")),
            string.Join("; ", precios.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")));
    }

    /// <summary>Cada fila de precio, completa: lo que el recálculo escribe y lo que no debe tocar.</summary>
    private static async Task<Dictionary<(int Articulo, string Modalidad), (string? Base, decimal? Porcentaje, decimal? Precio, bool Manual, DateTime? Fecha, int? Usuario)>> PreciosAsync(LaCentralDbContext contexto) =>
        (await contexto.ArticuloPrecioModalidad.AsNoTracking().ToListAsync()).ToDictionary(
            p => (p.ArticuloId, p.Modalidad),
            p => (p.Base, p.Porcentaje, p.Precio, p.CalculoManual, p.FechaCalculo, p.UsuarioId));

    private static async Task<Dictionary<(int Articulo, short Sucursal), decimal>> StockAsync(LaCentralDbContext contexto) =>
        (await contexto.Stock.AsNoTracking().ToListAsync()).ToDictionary(s => (s.ArticuloId, s.SucursalId), s => s.Cantidad);

    private static async Task<Dictionary<int, (decimal? Costo, int? UltimoProveedor)>> ArticulosAsync(LaCentralDbContext contexto) =>
        (await contexto.Articulo.AsNoTracking().ToListAsync()).ToDictionary(a => a.Id, a => (a.PrecioCosto, a.UltimoProveedorId));
}

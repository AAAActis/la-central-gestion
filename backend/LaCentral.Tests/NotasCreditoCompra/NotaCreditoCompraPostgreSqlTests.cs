using LaCentral.Api.Seguridad;
using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.NotasCreditoCompra;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>
/// HU-FCO-07 (057) contra PostgreSQL real (plantilla con el script 11), con los casos de uso, los
/// repositorios y la unidad de trabajo de producción. Las facturas se cargan con el registro real
/// (HU-FCO-01) y todo se relee con un contexto nuevo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class NotaCreditoCompraPostgreSqlTests
{
    private const string ClaveJefa = "clave-de-la-jefa";
    private readonly ITestOutputHelper _salida;
    public NotaCreditoCompraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    /// <summary>
    /// F1 (P1): 5 de X a 100 en S1 y 2 de Y a 40 en S2. F2 (P1): 3 de X a 120 en S1. FP2 (P2): 1 de X.
    /// Después de las facturas: X con 9 en S1 y costo de FP2; Y con 2 en S2. RC es el depósito Reclamos.
    /// </summary>
    private sealed record Escenario(PostgreSqlPruebas Fixture, int Operador, int P1, int P2, int X, int Y,
        short S1, short S2, short RC, int F1, int F2, int FP2);

    private static readonly DateTime CalculoAnterior = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private static List<ArticuloPrecioModalidad> Precios(decimal? costo) =>
        CalcularPreciosArticuloUseCase.Recalcular(ReglasPrecioModalidad.ConfiguracionInicial(), costo, CalculoAnterior)
            .Select(p => new ArticuloPrecioModalidad
            {
                Modalidad = p.Modalidad.ToString(), Base = p.Base?.ToString(), Porcentaje = p.Porcentaje,
                Precio = p.Precio, CalculoManual = p.CalculoManual, FechaCalculo = p.FechaCalculo
            }).ToList();

    private static async Task<Escenario> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var operador = await fixture.CrearUsuarioAsync("operador.fco07");
        int p1, p2, x, y;
        short s1, s2, rc;
        await using (var contexto = fixture.CrearContexto())
        {
            var sucursales = await contexto.Sucursal.Where(s => !s.EsReclamos).OrderBy(s => s.Id).Select(s => s.Id).Take(2).ToArrayAsync();
            (s1, s2) = (sucursales[0], sucursales[1]);
            rc = await contexto.Sucursal.Where(s => s.EsReclamos).Select(s => s.Id).SingleAsync();
            contexto.Usuario.Add(new Usuario
            {
                NombreUsuario = "jefa.fco07", HashContrasena = new ServicioHash().HashearClave(ClaveJefa),
                RolId = await contexto.Rol.Where(r => r.Nombre == "ADMINISTRADOR").Select(r => r.Id).SingleAsync(),
                SucursalId = s1, Activo = true
            });
            var proveedor1 = new Proveedor { Codigo = "PRV-N1", RazonSocial = "Proveedor uno", Activo = true };
            var proveedor2 = new Proveedor { Codigo = "PRV-N2", RazonSocial = "Proveedor dos", Activo = true };
            var articuloX = new Articulo { CodigoInterno = "NC-X", Nombre = "Artículo X", Activo = true, ArticuloPrecioModalidad = Precios(null) };
            var articuloY = new Articulo { CodigoInterno = "NC-Y", Nombre = "Artículo Y", Activo = true, ArticuloPrecioModalidad = Precios(null) };
            contexto.AddRange(proveedor1, proveedor2, articuloX, articuloY);
            await contexto.SaveChangesAsync();
            (p1, p2, x, y) = (proveedor1.Id, proveedor2.Id, articuloX.Id, articuloY.Id);
        }
        var e = new Escenario(fixture, operador, p1, p2, x, y, s1, s2, rc, 0, 0, 0);
        var f1 = await RegistrarFacturaAsync(e, p1, "A-0001-00000001",
            new LineaFacturaCompraRequest(x, s1, 5m, 100m, 0m), new LineaFacturaCompraRequest(y, s2, 2m, 40m, 0m));
        var f2 = await RegistrarFacturaAsync(e, p1, "A-0001-00000002", new LineaFacturaCompraRequest(x, s1, 3m, 120m, 0m));
        var fp2 = await RegistrarFacturaAsync(e, p2, "A-0002-00000001", new LineaFacturaCompraRequest(x, s1, 1m, 90m, 0m));
        return e with { F1 = f1, F2 = f2, FP2 = fp2 };
    }

    private static Mock<IContextoUsuario> Sesion(Escenario e)
    {
        var sesion = new Mock<IContextoUsuario>();
        sesion.SetupGet(s => s.UsuarioId).Returns(e.Operador);
        sesion.SetupGet(s => s.SucursalId).Returns(e.S1);
        return sesion;
    }

    private static async Task<int> RegistrarFacturaAsync(Escenario e, int proveedor, string numero, params LineaFacturaCompraRequest[] lineas)
    {
        await using var contexto = e.Fixture.CrearContexto();
        var resultado = await new RegistrarFacturaCompraUseCase(new FacturaCompraRepositorio(contexto), new ProveedorRepositorio(contexto),
                new ArticuloRepositorio(contexto), new UnidadDeTrabajo(contexto), Sesion(e).Object,
                new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(contexto)))
            .EjecutarAsync(new RegistrarFacturaCompraRequest(proveedor, numero, new DateOnly(2026, 10, 1), null, 0m, 0m, 0m, 0m,
                lineas, TipoComprobante: "A"));
        Assert.True(resultado.IsSuccess, resultado.Error);
        return resultado.Value.Id;
    }

    private static RegistrarNotaCreditoCompraUseCase CasoRegistrar(Escenario e, LaCentralDbContext contexto) =>
        new(new NotaCreditoCompraRepositorio(contexto), new ProveedorRepositorio(contexto), new FacturaCompraRepositorio(contexto),
            new ArticuloRepositorio(contexto), new UnidadDeTrabajo(contexto), Sesion(e).Object);

    private static RegistrarNotaCreditoCompraRequest Devolucion(Escenario e, string numero, int[] facturas,
        params LineaNotaCreditoCompraRequest[] lineas) =>
        new(e.P1, numero, new DateOnly(2026, 11, 5), "DEVOLUCION", 100m, null, null, null, null, facturas, lineas);

    private static async Task<Result<RegistrarNotaCreditoCompraResponse>> RegistrarAsync(Escenario e, RegistrarNotaCreditoCompraRequest request)
    {
        await using var contexto = e.Fixture.CrearContexto();
        return await CasoRegistrar(e, contexto).EjecutarAsync(request);
    }

    private static async Task<Result> AnularAsync(Escenario e, int nota, string motivo = "Cargada dos veces")
    {
        await using var contexto = e.Fixture.CrearContexto();
        return await new AnularNotaCreditoCompraUseCase(new NotaCreditoCompraRepositorio(contexto), new UsuarioRepositorio(contexto),
                new ServicioHash(), new UnidadDeTrabajo(contexto), Sesion(e).Object)
            .EjecutarAsync(nota, new AnularNotaCreditoCompraRequest(motivo, "jefa.fco07", ClaveJefa));
    }

    private static async Task<decimal?> StockAsync(Escenario e, int articulo, short sucursal)
    {
        await using var c = e.Fixture.CrearContexto();
        return await c.Stock.AsNoTracking().Where(s => s.ArticuloId == articulo && s.SucursalId == sucursal)
            .Select(s => (decimal?)s.Cantidad).SingleOrDefaultAsync();
    }

    // CA-004: la bonificación escribe la cabecera y la relación con las facturas, y no toca stock ni costo.
    [PostgreSqlFact]
    public async Task Bonificacion_EscribeCabeceraYFacturas_SinLineasNiStock_NiCosto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        decimal? costoAntes;
        await using (var c = fixture.CrearContexto())
            costoAntes = (await c.Articulo.AsNoTracking().SingleAsync(a => a.Id == e.X)).PrecioCosto;

        var resultado = await RegistrarAsync(e, new RegistrarNotaCreditoCompraRequest(e.P1, " 0010-00021495 ", new DateOnly(2026, 11, 5),
            "bonificacion", 115040m, 10.5m, 20.25m, 3m, 4m, [e.F1, e.F2], null, DesdePdf: true));

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        var fila = await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == resultado.Value.Id);
        Assert.Equal((e.P1, "0010-00021495", new DateOnly(2026, 11, 5), "BONIFICACION", 115040m, 10.5m, 20.25m, 3m, 4m, "PDF", e.Operador, "ACTIVA"),
            (fila.ProveedorId, fila.Numero, fila.FechaEmision, fila.Clase, fila.ImporteNeto, fila.PercepcionIva, fila.PercepcionIibb,
             fila.PercepcionMunicipal, fila.PercepcionGanancias, fila.Origen, fila.UsuarioId, fila.Estado));
        Assert.True(fila.FechaRegistro > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(new[] { e.F1, e.F2 }.Order(),
            await relectura.NotaCreditoCompraFactura.Where(f => f.NotaCreditoCompraId == fila.Id).Select(f => f.FacturaCompraId).OrderBy(f => f).ToListAsync());
        Assert.False(await relectura.NotaCreditoCompraDetalle.AnyAsync());
        Assert.Equal(9m, await StockAsync(e, e.X, e.S1));
        Assert.Equal(costoAntes, (await relectura.Articulo.AsNoTracking().SingleAsync(a => a.Id == e.X)).PrecioCosto);
    }

    // CA-005: la devolución escribe sus líneas y resta del origen de cada una, también de Reclamos
    // (sin fila previa: queda en negativo, D-S6-24). No cambia costo, precios ni historial de compra.
    [PostgreSqlFact]
    public async Task Devolucion_EscribeLineasYRestaDelOrigen_SinTocarCostoPreciosNiHistorial()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        (decimal? Costo, int? Proveedor, decimal? Lista, int Historial) antes, despues;
        await using (var c = fixture.CrearContexto())
            antes = await FotoArticuloAsync(c, e.X);

        var resultado = await RegistrarAsync(e, Devolucion(e, "0010-00000001", [e.F1],
            new LineaNotaCreditoCompraRequest(e.X, e.S1, 2m), new LineaNotaCreditoCompraRequest(e.X, e.RC, 1.5m),
            new LineaNotaCreditoCompraRequest(e.Y, e.S2, 0.25m)));

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        var fila = await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == resultado.Value.Id);
        Assert.Equal(("DEVOLUCION", "MANUAL"), (fila.Clase, fila.Origen));
        var lineas = await relectura.NotaCreditoCompraDetalle.AsNoTracking().Where(d => d.NotaCreditoCompraId == fila.Id)
            .OrderBy(d => d.ArticuloId).ThenBy(d => d.SucursalId).Select(d => new { d.ArticuloId, d.SucursalId, d.Cantidad }).ToListAsync();
        Assert.Equal(new[] { new { ArticuloId = e.X, SucursalId = e.S1, Cantidad = 2m }, new { ArticuloId = e.X, SucursalId = e.RC, Cantidad = 1.5m },
            new { ArticuloId = e.Y, SucursalId = e.S2, Cantidad = 0.25m } }.OrderBy(l => l.ArticuloId).ThenBy(l => l.SucursalId), lineas);
        Assert.Equal(7m, await StockAsync(e, e.X, e.S1));
        Assert.Equal(-1.5m, await StockAsync(e, e.X, e.RC));
        Assert.Equal(1.75m, await StockAsync(e, e.Y, e.S2));
        despues = await FotoArticuloAsync(relectura, e.X);
        Assert.Equal(antes, despues);
    }

    private static async Task<(decimal?, int?, decimal?, int)> FotoArticuloAsync(LaCentralDbContext c, int articulo)
    {
        var a = await c.Articulo.AsNoTracking().SingleAsync(x => x.Id == articulo);
        return (a.PrecioCosto, a.UltimoProveedorId,
            await c.ArticuloPrecioModalidad.AsNoTracking().Where(p => p.ArticuloId == articulo && p.Modalidad == "Lista").Select(p => p.Precio).SingleAsync(),
            await c.ArticuloHistorialCompra.CountAsync(h => h.ArticuloId == articulo));
    }

    // CA-001: las facturas pedidas que existen, con su proveedor y si están activas.
    [PostgreSqlFact]
    public async Task ObtenerFacturas_DevuelveLasQueExisten_ConProveedorYEstado()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var c = fixture.CrearContexto())
        {
            var jefa = await c.Usuario.Where(u => u.NombreUsuario == "jefa.fco07").Select(u => u.Id).SingleAsync();
            await c.FacturaCompra.Where(f => f.Id == e.F2).ExecuteUpdateAsync(s => s
                .SetProperty(f => f.Estado, "ANULADA").SetProperty(f => f.MotivoAnulacion, "x")
                .SetProperty(f => f.FechaAnulacion, DateTime.UtcNow).SetProperty(f => f.UsuarioAnulacionId, e.Operador)
                .SetProperty(f => f.UsuarioAutorizacionId, jefa));
        }

        await using var contexto = fixture.CrearContexto();
        var facturas = await new NotaCreditoCompraRepositorio(contexto).ObtenerFacturasAsync([e.F1, e.F2, e.FP2, e.FP2 + 1000]);

        Assert.Equal(new[] { new FacturaParaNotaCredito(e.F1, e.P1, true), new FacturaParaNotaCredito(e.F2, e.P1, false),
            new FacturaParaNotaCredito(e.FP2, e.P2, true) }, facturas.OrderBy(f => f.Id));
    }

    // CA-014 (D-S6-25): lo devuelto se acumula por artículo sobre lo comprado en las facturas
    // referenciadas, sin contar las notas anuladas; un artículo que no está en ellas no se devuelve.
    [PostgreSqlFact]
    public async Task Cantidades_SeAcumulanSobreLoComprado_SinContarLasAnuladas()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var primera = await RegistrarAsync(e, Devolucion(e, "NC-1", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 3m)));
        Assert.True(primera.IsSuccess, primera.Error);
        // En F1 y F2 se compraron 8 de X: ya se devolvieron 3 (la NC-1 referencia F1).
        await using (var c = fixture.CrearContexto())
            Assert.Equal(new[] { new CompradoYDevuelto(e.X, 8m, 3m), new CompradoYDevuelto(e.Y, 2m, 0m) }.OrderBy(x => x.ArticuloId),
                (await new NotaCreditoCompraRepositorio(c).ObtenerCompradoYDevueltoAsync([e.F1, e.F2])).OrderBy(x => x.ArticuloId));

        var excedida = await RegistrarAsync(e, Devolucion(e, "NC-2", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 2.5m)));
        Assert.Equal(TipoError.Invalido, excedida.Tipo);
        Assert.Contains("hasta 2", excedida.Error);

        var fueraDeLaFactura = await RegistrarAsync(e, Devolucion(e, "NC-3", [e.F2], new LineaNotaCreditoCompraRequest(e.Y, e.S2, 1m)));
        Assert.Equal(TipoError.Invalido, fueraDeLaFactura.Tipo);

        Assert.True((await AnularAsync(e, primera.Value.Id)).IsSuccess);
        var despuesDeAnular = await RegistrarAsync(e, Devolucion(e, "NC-2", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 5m)));
        Assert.True(despuesDeAnular.IsSuccess, despuesDeAnular.Error);

        await using var relectura = fixture.CrearContexto();
        Assert.Equal(2, await relectura.NotaCreditoCompra.CountAsync());
        Assert.Equal(4m, await StockAsync(e, e.X, e.S1));
    }

    // CA-014 (D-S6-25): dos devoluciones simultáneas sobre la misma factura no pasan juntas el límite.
    // La primera tiene la factura bloqueada; la segunda espera ese bloqueo (se ve en pg_stat_activity)
    // y, cuando la primera confirma, ve lo devuelto y se rechaza.
    [PostgreSqlFact]
    public async Task DosDevolucionesSimultaneas_LaSegundaEsperaYSeRechaza()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using var primera = fixture.CrearContexto();
        await using var segunda = fixture.CrearContexto();
        await segunda.Database.OpenConnectionAsync();
        var pidSegunda = await segunda.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();

        var repositorio = new NotaCreditoCompraRepositorio(primera);
        await using var transaccion = await primera.Database.BeginTransactionAsync();
        Assert.Contains(new CompradoYDevuelto(e.X, 5m, 0m), await repositorio.ObtenerCompradoYDevueltoAsync([e.F1]));

        var enEspera = CasoRegistrar(e, segunda).EjecutarAsync(Devolucion(e, "NC-B", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 4m)));
        await EsperarBloqueoAsync(fixture, pidSegunda, enEspera);

        var escrita = await repositorio.RegistrarConEfectosAsync(new LaCentral.UseCases.Entidades.NotaCreditoCompra
        {
            ProveedorId = e.P1, Numero = "NC-A", FechaEmision = new DateOnly(2026, 11, 5), Clase = "DEVOLUCION", ImporteNeto = 100m,
            UsuarioId = e.Operador, FechaRegistro = DateTime.UtcNow, FacturaIds = [e.F1],
            Lineas = [new() { ArticuloId = e.X, SucursalId = e.S1, Cantidad = 4m }]
        });
        Assert.True(escrita.IsSuccess, escrita.Error);
        await transaccion.CommitAsync();

        var rechazada = await enEspera;
        Assert.Equal(TipoError.Invalido, rechazada.Tipo);
        Assert.Contains("hasta 1", rechazada.Error);
        await using var relectura = fixture.CrearContexto();
        Assert.Equal(new[] { "NC-A" }, await relectura.NotaCreditoCompra.Select(n => n.Numero).ToListAsync());
        Assert.Equal(5m, await StockAsync(e, e.X, e.S1));
    }

    private static async Task EsperarBloqueoAsync(PostgreSqlPruebas fixture, int pid, Task enEspera)
    {
        await using var observador = fixture.CrearContexto();
        for (var intento = 0; intento < 300; intento++)
        {
            Assert.False(enEspera.IsCompleted, "La operación en espera terminó sin esperar el bloqueo de la factura.");
            var espera = await observador.Database.SqlQuery<string?>($"SELECT wait_event_type AS \"Value\" FROM pg_stat_activity WHERE pid = {pid}").SingleOrDefaultAsync();
            if (espera == "Lock")
                return;
            await Task.Delay(100);
        }
        Assert.Fail("La operación en espera no llegó a esperar el bloqueo en 30 s.");
    }

    // CA-012 (D-S6-23): la anulación escribe la doble certificación, devuelve el stock a cada origen
    // y libera el número; una segunda anulación de la misma nota es Conflicto.
    [PostgreSqlFact]
    public async Task Anular_DevuelveElStockAlOrigen_EscribeQuienYLiberaElNumero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var nota = await RegistrarAsync(e, Devolucion(e, "NC-9", [e.F1],
            new LineaNotaCreditoCompraRequest(e.X, e.RC, 2m), new LineaNotaCreditoCompraRequest(e.Y, e.S2, 1m)));
        Assert.True(nota.IsSuccess, nota.Error);
        await using (var c = fixture.CrearContexto())
        {
            var repositorio = new NotaCreditoCompraRepositorio(c);
            Assert.True(await repositorio.ExisteNumeroAsync(e.P1, "NC-9"));
            Assert.False(await repositorio.ExisteNumeroAsync(e.P2, "NC-9"));
            Assert.Equal(new NotaCreditoParaAnular(nota.Value.Id, "NC-9", true), await repositorio.ObtenerParaAnularAsync(nota.Value.Id));
            Assert.Null(await repositorio.ObtenerParaAnularAsync(nota.Value.Id + 1000));
        }

        var resultado = await AnularAsync(e, nota.Value.Id, " Mal cargada ");

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var relectura = fixture.CrearContexto();
        var fila = await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == nota.Value.Id);
        var jefa = await relectura.Usuario.Where(u => u.NombreUsuario == "jefa.fco07").Select(u => u.Id).SingleAsync();
        Assert.Equal(("ANULADA", "Mal cargada", e.Operador, jefa), (fila.Estado, fila.MotivoAnulacion, fila.UsuarioAnulacionId, fila.UsuarioAutorizacionId));
        Assert.True(fila.FechaAnulacion > DateTime.UtcNow.AddMinutes(-5));
        Assert.Equal(0m, await StockAsync(e, e.X, e.RC));
        Assert.Equal(2m, await StockAsync(e, e.Y, e.S2));
        var repositorioNuevo = new NotaCreditoCompraRepositorio(relectura);
        Assert.False(await repositorioNuevo.ExisteNumeroAsync(e.P1, "NC-9"));
        Assert.Equal(new NotaCreditoParaAnular(nota.Value.Id, "NC-9", false), await repositorioNuevo.ObtenerParaAnularAsync(nota.Value.Id));

        var otraVez = await repositorioNuevo.AnularConEfectosAsync(new AnulacionNotaCreditoCompra(nota.Value.Id, "x", e.Operador, jefa, DateTime.UtcNow));
        Assert.Equal(TipoError.Conflicto, otraVez.Tipo);
        Assert.Equal(2m, await StockAsync(e, e.Y, e.S2));
        Assert.True((await RegistrarAsync(e, Devolucion(e, "NC-9", [e.F1], new LineaNotaCreditoCompraRequest(e.Y, e.S2, 1m)))).IsSuccess);
    }

    // Si la resta o la suma no entra en stock.cantidad (NUMERIC(12,3)), Invalido y no queda nada.
    [PostgreSqlFact]
    public async Task StockFueraDeRango_EsInvalido_YNoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var c = fixture.CrearContexto())
            await c.Stock.Where(s => s.ArticuloId == e.X && s.SucursalId == e.S1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, -999_999_999m));

        var registro = await RegistrarAsync(e, Devolucion(e, "NC-R", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 1m)));
        Assert.Equal(TipoError.Invalido, registro.Tipo);
        await using (var c = fixture.CrearContexto())
            Assert.False(await c.NotaCreditoCompra.AnyAsync());

        await using (var c = fixture.CrearContexto())
            await c.Stock.Where(s => s.ArticuloId == e.X && s.SucursalId == e.S1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 5m));
        var nota = await RegistrarAsync(e, Devolucion(e, "NC-R", [e.F1], new LineaNotaCreditoCompraRequest(e.X, e.S1, 1m)));
        Assert.True(nota.IsSuccess, nota.Error);
        await using (var c = fixture.CrearContexto())
            await c.Stock.Where(s => s.ArticuloId == e.X && s.SucursalId == e.S1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 999_999_999.5m));

        var anulacion = await AnularAsync(e, nota.Value.Id);
        Assert.Equal(TipoError.Invalido, anulacion.Tipo);
        await using var relectura = fixture.CrearContexto();
        Assert.Equal("ACTIVA", (await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync()).Estado);
        Assert.Equal(999_999_999.5m, await StockAsync(e, e.X, e.S1));
    }

    // CA-011 (D-S6-22): la factura con una nota vigente no se anula, y el mensaje la nombra;
    // anulada la nota, la factura sí.
    [PostgreSqlFact]
    public async Task AnularFactura_ConNotaVigente_NoSeAnula_YConLaNotaAnuladaSi()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var nota = await RegistrarAsync(e, new RegistrarNotaCreditoCompraRequest(e.P1, "0010-00000077", new DateOnly(2026, 11, 5),
            "BONIFICACION", 10m, null, null, null, null, [e.F2], null));
        Assert.True(nota.IsSuccess, nota.Error);

        var rechazada = await AnularFacturaAsync(e, e.F2);

        Assert.Equal(TipoError.Conflicto, rechazada.Tipo);
        Assert.Contains("0010-00000077", rechazada.Error);
        await using (var c = fixture.CrearContexto())
            Assert.Equal("ACTIVA", (await c.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == e.F2)).Estado);

        Assert.True((await AnularAsync(e, nota.Value.Id)).IsSuccess);
        var anulada = await AnularFacturaAsync(e, e.F2);
        Assert.True(anulada.IsSuccess, anulada.Error);
        await using var relectura = fixture.CrearContexto();
        Assert.Equal("ANULADA", (await relectura.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == e.F2)).Estado);
    }

    private static async Task<Result> AnularFacturaAsync(Escenario e, int factura)
    {
        await using var contexto = e.Fixture.CrearContexto();
        return await new AnularFacturaCompraUseCase(new FacturaCompraRepositorio(contexto), new UsuarioRepositorio(contexto),
                new ServicioHash(), new UnidadDeTrabajo(contexto), Sesion(e).Object,
                new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(contexto)), new NotaCreditoCompraRepositorio(contexto))
            .EjecutarAsync(factura, new AnularFacturaCompraRequest("Cargada dos veces", "jefa.fco07", ClaveJefa));
    }

    // El registro vuelve a mirar las facturas dentro de la transacción, con ellas bloqueadas: si una
    // se anuló después de la validación previa, Conflicto y no se escribe nada (la otra mitad del CA-011).
    [PostgreSqlFact]
    public async Task Registrar_ConUnaFacturaAnuladaDespuesDeValidar_EsConflicto_YNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Assert.True((await AnularFacturaAsync(e, e.F2)).IsSuccess);

        await using var contexto = fixture.CrearContexto();
        var escrita = await new UnidadDeTrabajo(contexto).EjecutarAsync(async ct =>
            await new NotaCreditoCompraRepositorio(contexto).RegistrarConEfectosAsync(new LaCentral.UseCases.Entidades.NotaCreditoCompra
            {
                ProveedorId = e.P1, Numero = "NC-T", FechaEmision = new DateOnly(2026, 11, 5), Clase = "DEVOLUCION", ImporteNeto = 100m,
                UsuarioId = e.Operador, FechaRegistro = DateTime.UtcNow, FacturaIds = [e.F1, e.F2],
                Lineas = [new() { ArticuloId = e.X, SucursalId = e.S1, Cantidad = 1m }]
            }, ct) is { IsSuccess: false } fallo ? Result.Failure(fallo.Tipo, fallo.Error) : Result.Success());

        Assert.Equal(TipoError.Conflicto, escrita.Tipo);
        Assert.Contains("A-0001-00000002", escrita.Error);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.NotaCreditoCompra.AnyAsync());
        Assert.Equal(6m, await StockAsync(e, e.X, e.S1));
    }

    // CA-011 con concurrencia: mientras se anula la factura (que la tiene bloqueada desde que pidió
    // las notas vigentes), una nota sobre esa factura espera y, al confirmarse la anulación, se rechaza.
    [PostgreSqlFact]
    public async Task NotaMientrasSeAnulaLaFactura_EsperaYSeRechaza()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using var anulacion = fixture.CrearContexto();
        await using var registro = fixture.CrearContexto();
        await registro.Database.OpenConnectionAsync();
        var pidRegistro = await registro.Database.SqlQueryRaw<int>("SELECT pg_backend_pid() AS \"Value\"").SingleAsync();
        int jefa;
        await using (var c = fixture.CrearContexto())
            jefa = await c.Usuario.Where(u => u.NombreUsuario == "jefa.fco07").Select(u => u.Id).SingleAsync();

        await using var transaccion = await anulacion.Database.BeginTransactionAsync();
        Assert.Empty(await new NotaCreditoCompraRepositorio(anulacion).ObtenerVigentesDeFacturaAsync(e.F2));

        var enEspera = CasoRegistrar(e, registro).EjecutarAsync(new RegistrarNotaCreditoCompraRequest(e.P1, "NC-C", new DateOnly(2026, 11, 5),
            "BONIFICACION", 10m, null, null, null, null, [e.F2], null));
        await EsperarBloqueoAsync(fixture, pidRegistro, enEspera);

        Assert.True((await new FacturaCompraRepositorio(anulacion).AnularConEfectosAsync(
            new AnulacionFacturaCompra(e.F2, "Cargada dos veces", e.Operador, jefa, DateTime.UtcNow))).IsSuccess);
        await transaccion.CommitAsync();

        Assert.Equal(TipoError.Conflicto, (await enEspera).Tipo);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.NotaCreditoCompra.AnyAsync());
    }

    // CA-008: Reclamos no es destino de una factura de compra (sí origen de una devolución, arriba).
    [PostgreSqlFact]
    public async Task Factura_ConDestinoReclamos_SeRechaza()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        await using (var contexto = fixture.CrearContexto())
        {
            var resultado = await new RegistrarFacturaCompraUseCase(new FacturaCompraRepositorio(contexto), new ProveedorRepositorio(contexto),
                    new ArticuloRepositorio(contexto), new UnidadDeTrabajo(contexto), Sesion(e).Object,
                    new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(contexto)))
                .EjecutarAsync(new RegistrarFacturaCompraRequest(e.P1, "A-0001-00000099", new DateOnly(2026, 10, 1), null, 0m, 0m, 0m, 0m,
                    [new LineaFacturaCompraRequest(e.X, e.RC, 1m, 100m, 0m)], TipoComprobante: "A"));
            Assert.Equal(TipoError.Invalido, resultado.Tipo);
        }
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.FacturaCompra.AnyAsync(f => f.Numero == "A-0001-00000099"));
    }
}

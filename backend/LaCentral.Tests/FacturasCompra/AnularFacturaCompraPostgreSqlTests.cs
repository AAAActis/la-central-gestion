using LaCentral.Api.Seguridad;
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
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// HU-FCO-04 (028) contra PostgreSQL real, con el caso de uso, los repositorios y la unidad
/// de trabajo de producción. Las facturas se cargan con el registro real (HU-FCO-01) y todo
/// se relee con un contexto nuevo. CA-005: por cada punto de escritura, el motor falla ahí
/// (un trigger en el esquema aislado) y no queda ningún cambio.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class AnularFacturaCompraPostgreSqlTests
{
    private const string ClaveJefa = "clave-de-la-jefa";
    private readonly ITestOutputHelper _salida;
    public AnularFacturaCompraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    /// <summary>
    /// X: costo 50 sin último proveedor, 3 en la sucursal 1. Y: sin costo ni stock.
    /// F1 (P1): 2 de X a 100 en la 1. F2 (P2): 5 de X a 200 en la 1 y 2,5 de Y a 80 en la 2.
    /// </summary>
    private sealed record Escenario(PostgreSqlPruebas Fixture, int Operador, int Jefa, int P1, int P2,
        int X, int Y, short S1, short S2, int F1, int F2);

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
        var operador = await fixture.CrearUsuarioAsync("operador.fco04");
        int jefa, p1, p2, x, y;
        short[] sucursales;
        await using (var contexto = fixture.CrearContexto())
        {
            sucursales = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).Take(2).ToArrayAsync();
            var admin = new Usuario
            {
                NombreUsuario = "jefa.fco04", HashContrasena = new ServicioHash().HashearClave(ClaveJefa),
                RolId = await contexto.Rol.Where(r => r.Nombre == "ADMINISTRADOR").Select(r => r.Id).SingleAsync(),
                SucursalId = sucursales[0], Activo = true
            };
            var proveedor1 = new Proveedor { Codigo = "PRV-F1", RazonSocial = "Proveedor uno", Activo = true };
            var proveedor2 = new Proveedor { Codigo = "PRV-F2", RazonSocial = "Proveedor dos", Activo = true };
            var articuloX = new Articulo
            {
                CodigoInterno = "ANU-X", Nombre = "Artículo X", Activo = true, PrecioCosto = 50m,
                Stock = new List<Stock> { new() { SucursalId = sucursales[0], Cantidad = 3m } },
                ArticuloPrecioModalidad = Precios(50m)
            };
            var articuloY = new Articulo { CodigoInterno = "ANU-Y", Nombre = "Artículo Y", Activo = true, ArticuloPrecioModalidad = Precios(null) };
            contexto.AddRange(admin, proveedor1, proveedor2, articuloX, articuloY);
            await contexto.SaveChangesAsync();
            (jefa, p1, p2, x, y) = (admin.Id, proveedor1.Id, proveedor2.Id, articuloX.Id, articuloY.Id);
        }
        var e = new Escenario(fixture, operador, jefa, p1, p2, x, y, sucursales[0], sucursales[1], 0, 0);
        var f1 = await RegistrarAsync(e, p1, "A-0001-00000001", new LineaFacturaCompraRequest(x, e.S1, 2m, 100m, 0m));
        var f2 = await RegistrarAsync(e, p2, "A-0002-00000002",
            new LineaFacturaCompraRequest(x, e.S1, 5m, 200m, 0m), new LineaFacturaCompraRequest(y, e.S2, 2.5m, 80m, 0m));
        return e with { F1 = f1, F2 = f2 };
    }

    private static Mock<IContextoUsuario> Sesion(Escenario e)
    {
        var sesion = new Mock<IContextoUsuario>();
        sesion.SetupGet(s => s.UsuarioId).Returns(e.Operador);
        sesion.SetupGet(s => s.SucursalId).Returns(e.S1);
        return sesion;
    }

    private static async Task<int> RegistrarAsync(Escenario e, int proveedor, string numero, params LineaFacturaCompraRequest[] lineas)
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

    private static async Task<Result> AnularAsync(Escenario e, int factura, string motivo = "Cargada dos veces")
    {
        await using var contexto = e.Fixture.CrearContexto();
        return await new AnularFacturaCompraUseCase(new FacturaCompraRepositorio(contexto), new UsuarioRepositorio(contexto),
                new ServicioHash(), new UnidadDeTrabajo(contexto), Sesion(e).Object,
                new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(contexto)))
            .EjecutarAsync(factura, new AnularFacturaCompraRequest(motivo, "jefa.fco04", ClaveJefa));
    }

    private sealed record Foto(string EstadoF2, string? Motivo, int? Anulo, int? Autorizo, decimal StockXS1, decimal? StockYS2,
        decimal? CostoX, int? ProveedorX, decimal? CostoY, int? ProveedorY, decimal? ListaX, int HistorialAnulado);

    private static async Task<Foto> FotografiarAsync(Escenario e)
    {
        await using var c = e.Fixture.CrearContexto();
        var f2 = await c.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == e.F2);
        var stock = await c.Stock.AsNoTracking().ToListAsync();
        var x = await c.Articulo.AsNoTracking().SingleAsync(a => a.Id == e.X);
        var y = await c.Articulo.AsNoTracking().SingleAsync(a => a.Id == e.Y);
        return new(f2.Estado, f2.MotivoAnulacion, f2.UsuarioAnulacionId, f2.UsuarioAutorizacionId,
            stock.Single(s => s.ArticuloId == e.X && s.SucursalId == e.S1).Cantidad,
            stock.SingleOrDefault(s => s.ArticuloId == e.Y && s.SucursalId == e.S2)?.Cantidad,
            x.PrecioCosto, x.UltimoProveedorId, y.PrecioCosto, y.UltimoProveedorId,
            await c.ArticuloPrecioModalidad.AsNoTracking().Where(p => p.ArticuloId == e.X && p.Modalidad == "Lista").Select(p => p.Precio).SingleAsync(),
            await c.ArticuloHistorialCompra.CountAsync(h => h.Anulado));
    }

    // CA-001 y CA-002, y la doble certificación escrita en la fila.
    [PostgreSqlFact]
    public async Task Anular_RevierteStockCostoYProveedor_YDejaLaFacturaAnuladaConQuienAnuloYQuienAutorizo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Assert.Equal(new Foto("ACTIVA", null, null, null, 10m, 2.5m, 200m, e.P2, 80m, e.P2, 300m, 0), await FotografiarAsync(e));

        var resultado = await AnularAsync(e, e.F2, " Cargada dos veces ");

        Assert.True(resultado.IsSuccess, resultado.Error);
        // X vuelve al costo y al proveedor de F1, y su Lista se recalcula (100 × 1,50).
        // Y no tiene registro válido anterior: queda sin último proveedor y conserva su costo (CA-002).
        Assert.Equal(new Foto("ANULADA", "Cargada dos veces", e.Operador, e.Jefa, 5m, 0m, 100m, e.P1, 80m, null, 150m, 2),
            await FotografiarAsync(e));
        await using var c = fixture.CrearContexto();
        Assert.NotNull((await c.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == e.F2)).FechaAnulacion);
        Assert.Equal("ACTIVA", (await c.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == e.F1)).Estado);
        Assert.True(await c.ArticuloHistorialCompra.AllAsync(h => h.Anulado == (h.FacturaCompraId == e.F2)));
    }

    // CA-003: la mercadería ya se vendió; la reversión deja la existencia en negativo.
    [PostgreSqlFact]
    public async Task Anular_ConLaMercaderiaVendida_DejaElStockNegativo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var c = fixture.CrearContexto())
            await c.Stock.Where(s => s.ArticuloId == e.X && s.SucursalId == e.S1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Cantidad, 1m));

        Assert.True((await AnularAsync(e, e.F2)).IsSuccess);

        Assert.Equal(-4m, (await FotografiarAsync(e)).StockXS1);
    }

    // CA-004: el número de la factura anulada queda libre para ese proveedor.
    [PostgreSqlFact]
    public async Task Anular_LiberaElNumeroParaElMismoProveedor()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Assert.True((await AnularAsync(e, e.F2)).IsSuccess);

        var nueva = await RegistrarAsync(e, e.P2, "A-0002-00000002", new LineaFacturaCompraRequest(e.X, e.S1, 1m, 210m, 0m));

        await using var c = fixture.CrearContexto();
        Assert.Equal(2, await c.FacturaCompra.CountAsync(f => f.ProveedorId == e.P2 && f.Numero == "A-0002-00000002"));
        Assert.Equal("ACTIVA", (await c.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == nueva)).Estado);
    }

    // Anular dos veces: la segunda es Conflicto y no vuelve a restar stock.
    [PostgreSqlFact]
    public async Task AnularDosVeces_LaSegundaEsConflictoYNoRestaOtraVez()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Assert.True((await AnularAsync(e, e.F2)).IsSuccess);

        var segunda = await AnularAsync(e, e.F2);

        Assert.Equal(TipoError.Conflicto, segunda.Tipo);
        Assert.Equal(5m, (await FotografiarAsync(e)).StockXS1);
    }

    // CA-005: el motor falla en cada punto de escritura (en la SEGUNDA fila cuando hay dos)
    // y no queda ningún cambio: la factura sigue activa, sin cambios parciales.
    [PostgreSqlFact]
    public Task FallaAlRestarElStockDeLaSegundaLinea_NoQuedaNada()
        => AfirmarQueNoQuedaNadaAsync("stock", "UPDATE", "NEW.cantidad < OLD.cantidad AND OLD.sucursal_id <> (SELECT MIN(id) FROM sucursal)");

    [PostgreSqlFact]
    public Task FallaAlDevolverElCostoDelSegundoArticulo_NoQuedaNada()
        => AfirmarQueNoQuedaNadaAsync("articulo", "UPDATE", "NEW.codigo_interno = 'ANU-Y'");

    [PostgreSqlFact]
    public Task FallaAlAnularElSegundoRegistroDeHistorial_NoQuedaNada()
        => AfirmarQueNoQuedaNadaAsync("articulo_historial_compra", "UPDATE", "NEW.anulado AND NEW.precio_costo = 80");

    [PostgreSqlFact]
    public Task FallaAlMarcarLaFacturaAnulada_NoQuedaNada()
        => AfirmarQueNoQuedaNadaAsync("factura_compra", "UPDATE", "NEW.estado = 'ANULADA'");

    [PostgreSqlFact]
    public Task FallaAlRecalcularLosPrecios_NoQuedaNada()
        => AfirmarQueNoQuedaNadaAsync("articulo_precio_modalidad", "UPDATE", "TRUE");

    private async Task AfirmarQueNoQuedaNadaAsync(string tabla, string eventos, string condicion)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        var antes = await FotografiarAsync(e);
        await InstalarFallaAsync(fixture, tabla, eventos, condicion);

        // El fallo del motor puede subir como excepción o como Result en falla; las dos valen.
        Result? resultado = null;
        Exception? error = null;
        try { resultado = await AnularAsync(e, e.F2); }
        catch (Exception ex) { error = ex; }

        if (error is null) Assert.False(resultado!.IsSuccess, "la anulación no debía terminar bien");
        else Assert.Contains($"falla forzada en {tabla}", error.ToString());

        Assert.Equal(antes, await FotografiarAsync(e));
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
}

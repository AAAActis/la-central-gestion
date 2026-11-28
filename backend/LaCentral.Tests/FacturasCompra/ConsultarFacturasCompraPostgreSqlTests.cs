using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.FacturasCompra.Dtos;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>HU-FCO-02 (026): el filtrado y el orden, contra PostgreSQL real, con un contexto nuevo.</summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ConsultarFacturasCompraPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ConsultarFacturasCompraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private sealed record Escenario(PostgreSqlPruebas Fixture, int P1, int P2);

    /// <summary>
    /// P1: A-1 (emitida 01/09, vence 30/09), A-2 (15/10, vence 15/11, ANULADA), B-3 (01/11, sin vencimiento).
    /// P2: A-1 (20/10, vence 20/11). El mismo número en dos proveedores.
    /// </summary>
    private static async Task<Escenario> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuario = await fixture.CrearUsuarioAsync("consulta.fco02");
        await using var c = fixture.CrearContexto();
        var p1 = new Proveedor { Codigo = "PRV-C1", RazonSocial = "Lubricantes del Sur", Activo = true };
        var p2 = new Proveedor { Codigo = "PRV-C2", RazonSocial = "Frenos Norte", Activo = true };
        c.Proveedor.AddRange(p1, p2);
        await c.SaveChangesAsync();
        FacturaCompra Factura(int proveedor, string numero, DateOnly emision, DateOnly? vence, bool anulada = false) => new()
        {
            ProveedorId = proveedor, Numero = numero, FechaEmision = emision, FechaImputacion = emision, FechaVencimiento = vence,
            TipoComprobante = numero[0], Estado = anulada ? "ANULADA" : "ACTIVA", Origen = "MANUAL", UsuarioId = usuario,
            FechaRegistro = DateTime.UtcNow, MotivoAnulacion = anulada ? "Cargada por error" : null,
            FechaAnulacion = anulada ? DateTime.UtcNow : null,
            // el 13 (fc_anulacion_con_usuario) exige quién anuló
            UsuarioAnulacionId = anulada ? usuario : null
        };
        c.FacturaCompra.AddRange(
            Factura(p1.Id, "A-1", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)),
            Factura(p1.Id, "A-2", new DateOnly(2026, 10, 15), new DateOnly(2026, 11, 15), anulada: true),
            Factura(p1.Id, "B-3", new DateOnly(2026, 11, 1), null),
            Factura(p2.Id, "A-1", new DateOnly(2026, 10, 20), new DateOnly(2026, 11, 20)));
        await c.SaveChangesAsync();
        return new(fixture, p1.Id, p2.Id);
    }

    private static async Task<(IReadOnlyList<FacturaCompraResumen> Items, int Total)> BuscarAsync(
        Escenario e, FiltroFacturasCompra filtro, int pagina = 1, int tamano = 20)
    {
        await using var c = e.Fixture.CrearContexto();
        return await new FacturaCompraRepositorio(c).BuscarAsync(filtro, pagina, tamano);
    }

    // CA-001: número y proveedor devuelven exactamente una, aunque otro proveedor tenga el mismo número.
    [PostgreSqlFact]
    public async Task PorNumeroYProveedor_DevuelveExactamenteUna()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroFacturasCompra(e.P2, "A-1"));

        var una = Assert.Single(items);
        Assert.Equal(1, total);
        Assert.Equal((e.P2, "Frenos Norte", "A-1", "A", new DateOnly(2026, 10, 20), (DateOnly?)new DateOnly(2026, 11, 20), false),
            (una.ProveedorId, una.Proveedor, una.Numero, una.TipoComprobante, una.FechaEmision, una.FechaVencimiento, una.Anulada));
    }

    // CA-002 y CA-005: todas las del proveedor, la anulada marcada, de la más reciente a la más antigua.
    [PostgreSqlFact]
    public async Task PorProveedor_DevuelveTodas_ConLaAnuladaMarcada_DeLaMasReciente()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroFacturasCompra(e.P1));

        Assert.Equal(3, total);
        Assert.Equal(new[] { ("B-3", false), ("A-2", true), ("A-1", false) }, items.Select(f => (f.Numero, f.Anulada)));
    }

    // CA-003: rango de emisión, con los dos extremos incluidos.
    [PostgreSqlFact]
    public async Task PorRangoDeEmision_SoloLasDeEseRango_ExtremosIncluidos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, _) = await BuscarAsync(e, new FiltroFacturasCompra(EmisionDesde: new DateOnly(2026, 10, 15), EmisionHasta: new DateOnly(2026, 11, 1)));

        Assert.Equal(new[] { "B-3", "A-1", "A-2" }, items.Select(f => f.Numero));
        Assert.Equal(new[] { e.P1, e.P2, e.P1 }, items.Select(f => f.ProveedorId));
    }

    // CA-003: rango de vencimiento; la que no tiene vencimiento no entra.
    [PostgreSqlFact]
    public async Task PorRangoDeVencimiento_SoloLasDeEseRango_SinVencimientoNoEntra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, _) = await BuscarAsync(e, new FiltroFacturasCompra(VencimientoDesde: new DateOnly(2026, 9, 30), VencimientoHasta: new DateOnly(2026, 11, 15)));

        Assert.Equal(new[] { "A-2", "A-1" }, items.Select(f => f.Numero));
        Assert.All(items, f => Assert.Equal(e.P1, f.ProveedorId));
    }

    // CA-004: sin coincidencias es una página vacía (el mensaje lo pone la pantalla).
    [PostgreSqlFact]
    public async Task SinCoincidencias_PaginaVacia()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroFacturasCompra(e.P2, "B-3"));

        Assert.Empty(items);
        Assert.Equal(0, total);
    }

    [PostgreSqlFact]
    public async Task Pagina_TraeSoloSuTramo_YElTotalDeTodas()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroFacturasCompra(), pagina: 2, tamano: 3);

        Assert.Equal(4, total);
        Assert.Equal("A-1", Assert.Single(items).Numero);
        Assert.Equal(e.P1, items[0].ProveedorId);
    }
}

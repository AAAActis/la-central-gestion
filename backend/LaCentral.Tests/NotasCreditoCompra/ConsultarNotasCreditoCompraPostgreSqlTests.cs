using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using Xunit.Abstractions;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-012, el listado: filtrado, orden y paginación contra PostgreSQL real, con un contexto nuevo.</summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ConsultarNotasCreditoCompraPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ConsultarNotasCreditoCompraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private sealed record Escenario(PostgreSqlPruebas Fixture, int P1, int P2);

    /// <summary>
    /// P1: N-1 (bonificación, 01/09), N-2 (devolución, 15/10, ANULADA), N-3 (bonificación, 01/11).
    /// P2: N-1 (bonificación, 20/10). El mismo número en dos proveedores.
    /// </summary>
    private static async Task<Escenario> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuario = await fixture.CrearUsuarioAsync("consulta.nc");
        var autoriza = await fixture.CrearUsuarioAsync("autoriza.nc");
        await using var c = fixture.CrearContexto();
        var p1 = new Proveedor { Codigo = "PRV-NC1", RazonSocial = "Lubricantes del Sur", Activo = true };
        var p2 = new Proveedor { Codigo = "PRV-NC2", RazonSocial = "Frenos Norte", Activo = true };
        c.Proveedor.AddRange(p1, p2);
        await c.SaveChangesAsync();
        global::LaCentral.Data.Models.NotaCreditoCompra Nota(int proveedor, string numero, string clase, DateOnly emision, decimal neto, bool anulada = false) => new()
        {
            ProveedorId = proveedor, Numero = numero, FechaEmision = emision, Clase = clase, ImporteNeto = neto,
            Origen = "MANUAL", UsuarioId = usuario, FechaRegistro = DateTime.UtcNow, Estado = anulada ? "ANULADA" : "ACTIVA",
            MotivoAnulacion = anulada ? "Cargada por error" : null, FechaAnulacion = anulada ? DateTime.UtcNow : null,
            UsuarioAnulacionId = anulada ? usuario : null, UsuarioAutorizacionId = anulada ? autoriza : null
        };
        c.NotaCreditoCompra.AddRange(
            Nota(p1.Id, "N-1", "BONIFICACION", new DateOnly(2026, 9, 1), 100m),
            Nota(p1.Id, "N-2", "DEVOLUCION", new DateOnly(2026, 10, 15), 250.5m, anulada: true),
            Nota(p1.Id, "N-3", "BONIFICACION", new DateOnly(2026, 11, 1), 300m),
            Nota(p2.Id, "N-1", "BONIFICACION", new DateOnly(2026, 10, 20), 400m));
        await c.SaveChangesAsync();
        return new(fixture, p1.Id, p2.Id);
    }

    private static async Task<(IReadOnlyList<NotaCreditoCompraResumen> Items, int Total)> BuscarAsync(
        Escenario e, FiltroNotasCreditoCompra filtro, int pagina = 1, int tamano = 20)
    {
        await using var c = e.Fixture.CrearContexto();
        return await new NotaCreditoCompraRepositorio(c).BuscarAsync(filtro, pagina, tamano);
    }

    // Todas las del proveedor, la anulada marcada, de la más reciente a la más vieja, con sus datos.
    [PostgreSqlFact]
    public async Task PorProveedor_DevuelveTodas_ConLaAnuladaMarcada_DeLaMasReciente()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroNotasCreditoCompra(e.P1));

        Assert.Equal(3, total);
        Assert.Equal(new[] { ("N-3", false), ("N-2", true), ("N-1", false) }, items.Select(n => (n.Numero, n.Anulada)));
        var anulada = items[1];
        Assert.Equal((e.P1, "Lubricantes del Sur", "DEVOLUCION", new DateOnly(2026, 10, 15), 250.5m),
            (anulada.ProveedorId, anulada.Proveedor, anulada.Clase, anulada.FechaEmision, anulada.ImporteNeto));
    }

    // El rango de emisión incluye sus dos bordes; sin proveedor, trae de todos.
    [PostgreSqlFact]
    public async Task PorRangoDeEmision_IncluyeLosBordes_DeTodosLosProveedores()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroNotasCreditoCompra(EmisionDesde: new DateOnly(2026, 10, 15), EmisionHasta: new DateOnly(2026, 11, 1)));

        Assert.Equal(3, total);
        Assert.Equal(new[] { (e.P1, "N-3"), (e.P2, "N-1"), (e.P1, "N-2") }, items.Select(n => (n.ProveedorId, n.Numero)));
    }

    [PostgreSqlFact]
    public async Task ProveedorYRango_SeCombinan()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroNotasCreditoCompra(e.P2, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31)));

        Assert.Equal(1, total);
        Assert.Equal((e.P2, "N-1"), (Assert.Single(items).ProveedorId, items[0].Numero));
    }

    // La segunda página trae el resto, y el total es el de todas las páginas.
    [PostgreSqlFact]
    public async Task SinFiltro_PaginaDeTres_LaSegundaTraeLaUltima()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        var (items, total) = await BuscarAsync(e, new FiltroNotasCreditoCompra(), pagina: 2, tamano: 3);

        Assert.Equal(4, total);
        Assert.Equal((e.P1, "N-1"), (Assert.Single(items).ProveedorId, items[0].Numero));
    }
}

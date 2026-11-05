using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// N-9 · stock.ubicacion_deposito contra PostgreSQL real, con el repositorio de producción.
/// Todo se relee con un contexto nuevo.
/// </summary>
[Trait("Articulo", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class AsignarUbicacionPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public AsignarUbicacionPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private sealed record Escenario(PostgreSqlPruebas Fixture, short Sucursal1, short Sucursal2);

    private static async Task<Escenario> CrearEscenarioAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        var sucursales = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).Take(2).ToArrayAsync();
        return new(fixture, sucursales[0], sucursales[1]);
    }

    private static async Task<int> CrearArticuloAsync(Escenario e, string codigo, bool activo = true,
        params (short Sucursal, decimal Cantidad, string? Ubicacion)[] stock)
    {
        await using var contexto = e.Fixture.CrearContexto();
        var articulo = new Articulo
        {
            CodigoInterno = codigo, Nombre = $"Artículo {codigo}", Activo = activo,
            MotivoBaja = activo ? null : "Discontinuado", FechaBaja = activo ? null : DateTime.UtcNow,
            Stock = stock.Select(s => new Stock { SucursalId = s.Sucursal, Cantidad = s.Cantidad, UbicacionDeposito = s.Ubicacion }).ToList()
        };
        contexto.Articulo.Add(articulo);
        await contexto.SaveChangesAsync();
        return articulo.Id;
    }

    private static async Task<bool> AsignarAsync(Escenario e, int id, short sucursal, string? ubicacion)
    {
        await using var contexto = e.Fixture.CrearContexto();
        return await new ArticuloRepositorio(contexto).AsignarUbicacionAsync(id, sucursal, ubicacion);
    }

    private static async Task<(short Sucursal, decimal Cantidad, string? Ubicacion)[]> StockAsync(Escenario e, int id)
    {
        await using var relectura = e.Fixture.CrearContexto();
        return (await relectura.Stock.AsNoTracking().Where(s => s.ArticuloId == id).OrderBy(s => s.SucursalId).ToListAsync())
            .Select(s => (s.SucursalId, s.Cantidad, s.UbicacionDeposito)).ToArray();
    }

    [PostgreSqlFact]
    public async Task ConFilaDeStock_EscribeLaUbicacionSinTocarLaCantidadNiLaOtraSucursal()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await CrearEscenarioAsync(fixture);
        var id = await CrearArticuloAsync(e, "UBI-1", true, (e.Sucursal1, 5m, null), (e.Sucursal2, 2m, "Pasillo 3"));
        var otro = await CrearArticuloAsync(e, "UBI-2", true, (e.Sucursal1, 1m, null));

        Assert.True(await AsignarAsync(e, id, e.Sucursal1, "Estante 4"));

        Assert.Equal(new[] { (e.Sucursal1, 5m, (string?)"Estante 4"), (e.Sucursal2, 2m, (string?)"Pasillo 3") }, await StockAsync(e, id));
        Assert.Equal(new[] { (e.Sucursal1, 1m, (string?)null) }, await StockAsync(e, otro));
    }

    // Un artículo importado puede no tener fila de stock en una sucursal: la crea en cero.
    [PostgreSqlFact]
    public async Task SinFilaDeStock_LaCreaConCantidadCero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await CrearEscenarioAsync(fixture);
        var id = await CrearArticuloAsync(e, "UBI-1", true, (e.Sucursal1, 5m, null));

        Assert.True(await AsignarAsync(e, id, e.Sucursal2, "Estante 9"));

        Assert.Equal(new[] { (e.Sucursal1, 5m, (string?)null), (e.Sucursal2, 0m, (string?)"Estante 9") }, await StockAsync(e, id));
    }

    [PostgreSqlFact]
    public async Task Null_BorraLaUbicacion()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await CrearEscenarioAsync(fixture);
        var id = await CrearArticuloAsync(e, "UBI-1", true, (e.Sucursal1, 5m, "Estante 4"));

        Assert.True(await AsignarAsync(e, id, e.Sucursal1, null));

        Assert.Equal(new[] { (e.Sucursal1, 5m, (string?)null) }, await StockAsync(e, id));
    }

    // La condición "activo" va en la escritura: no crea ni modifica filas.
    [PostgreSqlFact]
    public async Task DadoDeBaja_FalseYNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await CrearEscenarioAsync(fixture);
        var id = await CrearArticuloAsync(e, "UBI-1", false, (e.Sucursal1, 5m, "Estante 4"));

        Assert.False(await AsignarAsync(e, id, e.Sucursal1, "Estante 7"));
        Assert.False(await AsignarAsync(e, id, e.Sucursal2, "Estante 7"));

        Assert.Equal(new[] { (e.Sucursal1, 5m, (string?)"Estante 4") }, await StockAsync(e, id));
    }

    // La ficha la muestra por sucursal; sin fila de stock, null (y cantidad cero, HU-STK-01).
    [PostgreSqlFact]
    public async Task Ficha_TraeLaUbicacionDeCadaSucursal()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await CrearEscenarioAsync(fixture);
        var id = await CrearArticuloAsync(e, "UBI-1", true, (e.Sucursal1, 5m, "Estante 4"));

        await using var relectura = fixture.CrearContexto();
        var detalle = await new ArticuloRepositorio(relectura).ObtenerDetallePorIdAsync(id);

        Assert.NotNull(detalle);
        Assert.Equal(new[] { (e.Sucursal1, 5m, (string?)"Estante 4"), (e.Sucursal2, 0m, (string?)null) },
            detalle.Existencias.Select(x => (x.SucursalId, x.Cantidad, x.UbicacionDeposito)));
    }
}

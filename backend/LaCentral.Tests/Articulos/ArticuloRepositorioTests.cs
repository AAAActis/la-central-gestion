using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.UseCases.Articulos.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Tests.Articulos;

public class ArticuloRepositorioTests
{
    private static LaCentralDbContext CrearContexto()
        => new(new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Articulo CrearArticulo(int id, string codigo, string nombre, bool activo = true)
        => new() { Id = id, CodigoInterno = codigo, Nombre = nombre, Activo = activo };

    [Fact]
    public async Task Busqueda_OrdenaInternoProveedorYNombreSinDuplicarPorAlternativas()
    {
        using var context = CrearContexto();
        var porNombre = CrearArticulo(1, "ART-1", "Mouse GENIUS 01");
        var porProveedor = CrearArticulo(2, "ART-2", "Mouse GENIUS 01");
        porProveedor.ArticuloCodigoAlternativo.Add(new() { Id = 1, ProveedorId = 1, Codigo = " gn-01 " });
        var porInterno = CrearArticulo(3, " GN-01 ", "Mouse GENIUS 01");
        var inactivo = CrearArticulo(4, "ART-4", "Mouse GENIUS 01", false);
        context.Articulo.AddRange(porNombre, porProveedor, porInterno, inactivo);
        await context.SaveChangesAsync();

        var pagina = await new ArticuloRepositorio(context).BuscarAsync(
            new("gn-01*mouse genius 01*GN-01*"));

        Assert.Equal(new[] { 3, 2, 1 }, pagina.Articulos.Select(a => a.Id));
        Assert.Equal(3, pagina.Total);
        var conflicto = Assert.Single(pagina.Conflictos);
        Assert.Equal(new[] { 2, 3 }, conflicto.ArticuloIds);
    }

    [Fact]
    public async Task Busqueda_PaginaDespuesDeUnirAlternativasYConservaTotalYOrdenEstable()
    {
        using var context = CrearContexto();
        context.Articulo.AddRange(Enumerable.Range(1, 5)
            .Select(i => CrearArticulo(i, $"ART-{i}", "Bujía NGK pequeña")));
        await context.SaveChangesAsync();
        var repo = new ArticuloRepositorio(context);
        var pagina = await repo.BuscarAsync(new("bujia NGK*bujía pequeña", Pagina: 2, TamanoPagina: 2));
        Assert.Equal(5, pagina.Total);
        Assert.Equal(new[] { 3, 4 }, pagina.Articulos.Select(a => a.Id));
        Assert.Equal(2, pagina.Pagina);
    }

    [Fact]
    public async Task Busqueda_PermiteInactivosYNoInterpretaComodinesSQL()
    {
        using var context = CrearContexto();
        context.Articulo.AddRange(CrearArticulo(1, "ART-1", "Filtro 100%_original", false),
            CrearArticulo(2, "ART-2", "Filtro común"));
        await context.SaveChangesAsync();
        var repo = new ArticuloRepositorio(context);
        Assert.Empty((await repo.BuscarAsync(new("100%_original"))).Articulos);
        Assert.Equal(1, Assert.Single((await repo.BuscarAsync(
            new("100%_original", IncluirInactivos: true))).Articulos).Id);
        Assert.Empty((await repo.BuscarAsync(new("inexistente"))).Articulos);
    }

    [Fact]
    public async Task Conflictos_IncluyeTodosLosIdsAunqueSeanInactivosOFueraDePagina()
    {
        using var context = CrearContexto();
        context.Articulo.AddRange(CrearArticulo(1, " gn-01 ", "Primero"),
            CrearArticulo(2, "GN-01", "Segundo", false));
        await context.SaveChangesAsync();
        var pagina = await new ArticuloRepositorio(context).BuscarAsync(new("gn-01", TamanoPagina: 1));
        Assert.Single(pagina.Articulos);
        Assert.Equal(new[] { 1, 2 }, Assert.Single(pagina.Conflictos).ArticuloIds);
    }

    [Fact]
    public async Task Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal()
    {
        using var context = CrearContexto();
        var articulo = CrearArticulo(1, "ART-1", "Mouse GENIUS");
        articulo.ArticuloCodigoAlternativo.Add(new()
        {
            Id = 1, Codigo = " gn-01 ", ProveedorId = 1,
            Proveedor = new() { Id = 1, Codigo = "PRV-1", RazonSocial = "Proveedor de prueba", Activo = true }
        });
        articulo.Stock.Add(new()
        {
            SucursalId = 1, Cantidad = 1.5m,
            Sucursal = new() { Id = 1, Codigo = "FR", Nombre = "Fragueiro" }
        });
        context.Articulo.Add(articulo);
        await context.SaveChangesAsync();
        var repo = new ArticuloRepositorio(context);
        var detalle = await repo.ObtenerDetallePorIdAsync(1);
        Assert.NotNull(detalle);
        Assert.Null(detalle.CostoVigente);
        Assert.Null(detalle.UltimoProveedorId);
        Assert.Null(detalle.PreciosPorModalidad);
        Assert.Equal(" gn-01 ", Assert.Single(detalle.Codigos).CodigoOriginal);
        Assert.Equal("GN-01", detalle.Codigos[0].ClaveNormalizada);
        Assert.Equal(1.5m, Assert.Single(detalle.Existencias).Cantidad);
        Assert.Null(await repo.ObtenerDetallePorIdAsync(999));
    }

    [Theory]
    [InlineData("* *", 1, 20)]
    [InlineData("mouse*\t", 1, 20)]
    [InlineData("mouse", 0, 20)]
    [InlineData("mouse", 1, 0)]
    [InlineData("mouse", 1, 101)]
    [InlineData("mouse", int.MaxValue, 100)]
    public void Consulta_RechazaEntradaOPaginaInvalida(string texto, int pagina, int tamano)
        => Assert.ThrowsAny<ArgumentException>(() => new ConsultaArticulos(texto, Pagina: pagina, TamanoPagina: tamano).Validar());

    [Fact]
    public void Consulta_TraduceRankingAgrupacionYPaginacionAPostgreSQL()
    {
        // ToQueryString traduce sin conectarse. No reemplaza ejecución PostgreSQL.
        using var context = new LaCentralDbContext(new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseNpgsql("Host=localhost;Database=traduccion_test;Username=solo_traduccion").Options);
        var sql = new ArticuloRepositorio(context).CrearPagina(new("gn-01*mouse genius", Pagina: 2)).ToQueryString();
        Assert.Contains("UNION ALL", sql);
        Assert.Contains("GROUP BY", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("LIMIT", sql);
        Assert.Contains("OFFSET", sql);
    }
}

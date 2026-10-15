using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.UseCases.Articulos.Dtos;
using Microsoft.EntityFrameworkCore;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

public class ArticuloRepositorioTests
{
    private readonly ITestOutputHelper _salida;
    public ArticuloRepositorioTests(ITestOutputHelper salida) => _salida = salida;

    // articulo_baja_coherente (D10): un artículo inactivo exige motivo y fecha de baja.
    private static Articulo CrearArticulo(string codigo, string nombre, bool activo = true)
        => new()
        {
            CodigoInterno = codigo, Nombre = nombre, Activo = activo,
            MotivoBaja = activo ? null : "Baja de prueba",
            FechaBaja = activo ? null : DateTime.UtcNow
        };

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Busqueda_OrdenaInternoProveedorYNombreSinDuplicarPorAlternativas()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var porNombre = CrearArticulo("ART-1", "Mouse GENIUS 01");
        var porProveedor = CrearArticulo("ART-2", "Mouse GENIUS 01");
        porProveedor.ArticuloCodigoAlternativo.Add(new() { Codigo = " gn-01 ", Proveedor = new() { Codigo = "PRV-01", RazonSocial = "Proveedor Test", Activo = true } });
        var porInterno = CrearArticulo(" GN-01 ", "Mouse GENIUS 01");
        var inactivo = CrearArticulo("ART-4", "Mouse GENIUS 01", false);
        context.Articulo.AddRange(porNombre, porProveedor, porInterno, inactivo);
        await context.SaveChangesAsync();

        var pagina = await new ArticuloRepositorio(context).BuscarAsync(
            new("gn-01*mouse genius 01*GN-01*"));

        Assert.Equal(new[] { porInterno.Id, porProveedor.Id, porNombre.Id }, pagina.Articulos.Select(a => a.Id));
        Assert.Equal(3, pagina.Total);
        var conflicto = Assert.Single(pagina.Conflictos);
        Assert.Equal(new[] { porProveedor.Id, porInterno.Id }.Order(), conflicto.ArticuloIds);
    }

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Busqueda_PaginaDespuesDeUnirAlternativasYConservaTotalYOrdenEstable()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var articulos = Enumerable.Range(1, 5).Select(i => CrearArticulo($"ART-{i}", "Bujía NGK pequeña")).ToArray();
        context.Articulo.AddRange(articulos);
        await context.SaveChangesAsync();
        var repo = new ArticuloRepositorio(context);
        var pagina = await repo.BuscarAsync(new("bujia NGK*bujía pequeña", Pagina: 2, TamanoPagina: 2));
        Assert.Equal(5, pagina.Total);
        Assert.Equal(articulos.OrderBy(a => a.Id).Skip(2).Take(2).Select(a => a.Id), pagina.Articulos.Select(a => a.Id));
        Assert.Equal(2, pagina.Pagina);
    }

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Busqueda_PermiteInactivosYNoInterpretaComodinesSQL()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var inactivo = CrearArticulo("ART-1", "Filtro 100%_original", false);
        context.Articulo.AddRange(inactivo,
            CrearArticulo("ART-2", "Filtro común"));
        await context.SaveChangesAsync();
        var repo = new ArticuloRepositorio(context);
        Assert.Empty((await repo.BuscarAsync(new("100%_original"))).Articulos);
        Assert.Equal(inactivo.Id, Assert.Single((await repo.BuscarAsync(
            new("100%_original", IncluirInactivos: true))).Articulos).Id);
        Assert.Empty((await repo.BuscarAsync(new("inexistente"))).Articulos);
    }

    // El caso EXACTO del plan, con los dos metacaracteres juntos. Los señuelos solo
    // coinciden si % y _ se interpretan: LIKE '%100%_ORIGINAL%' los trae a los tres.
    // Busqueda_PermiteInactivosYNoInterpretaComodinesSQL usa el mismo texto, pero su
    // señuelo ("Filtro común") no contiene "100": no discrimina.
    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Busqueda_100PorcientoGuionBajoOriginal_TraeSoloElLiteral()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var literal = CrearArticulo("ART-1", "Filtro 100%_original");
        context.Articulo.AddRange(literal,
            CrearArticulo("ART-2", "Filtro 100 de original"),
            CrearArticulo("ART-3", "Filtro 1005_original"));
        await context.SaveChangesAsync();

        var pagina = await new ArticuloRepositorio(context).BuscarAsync(new("100%_original"));

        Assert.Equal(literal.Id, Assert.Single(pagina.Articulos).Id);
        Assert.Equal(1, pagina.Total);
    }

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Busqueda_CadaResultadoTraeSusCodigosDeProveedor()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var norte = new Proveedor { Codigo = "PRV-N", RazonSocial = "Norte", Activo = true };
        var sur = new Proveedor { Codigo = "PRV-S", RazonSocial = "Sur", Activo = true };
        context.Proveedor.AddRange(norte, sur);
        await context.SaveChangesAsync();
        var conCodigos = CrearArticulo("ART-1", "Bujía NGK");
        conCodigos.ArticuloCodigoAlternativo.Add(new() { Codigo = " bpr6es ", ProveedorId = sur.Id });
        conCodigos.ArticuloCodigoAlternativo.Add(new() { Codigo = "NGK-6", ProveedorId = norte.Id });
        var sinCodigos = CrearArticulo("ART-2", "Bujía Bosch");
        context.Articulo.AddRange(conCodigos, sinCodigos);
        await context.SaveChangesAsync();

        var pagina = await new ArticuloRepositorio(context).BuscarAsync(new("bujia"));

        var primero = Assert.Single(pagina.Articulos, a => a.Id == conCodigos.Id);
        Assert.Equal("ART-1", primero.CodigoInterno);
        Assert.Equal(new[]
        {
            new CodigoProveedorResumenDto(norte.Id, "Norte", "NGK-6"),
            new CodigoProveedorResumenDto(sur.Id, "Sur", " bpr6es ")
        }.OrderBy(c => c.ProveedorId), primero.CodigosProveedor);
        Assert.Empty(Assert.Single(pagina.Articulos, a => a.Id == sinCodigos.Id).CodigosProveedor);
    }

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Conflictos_IncluyeTodosLosIdsAunqueSeanInactivosOFueraDePagina()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var primero = CrearArticulo(" gn-01 ", "Primero");
        var segundo = CrearArticulo("GN-01", "Segundo", false);
        context.Articulo.AddRange(primero, segundo);
        await context.SaveChangesAsync();
        var pagina = await new ArticuloRepositorio(context).BuscarAsync(new("gn-01", TamanoPagina: 1));
        Assert.Single(pagina.Articulos);
        Assert.Equal(new[] { primero.Id, segundo.Id }.Order(), Assert.Single(pagina.Conflictos).ArticuloIds);
    }

    [PostgreSqlFact]
    [Trait("Proveedor", "PostgreSQL")]
    public async Task Detalle_ConservaCostoNuloOriginalDelCodigoYStockDecimalPorSucursal()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var articulo = CrearArticulo("ART-1", "Mouse GENIUS");
        articulo.ArticuloCodigoAlternativo.Add(new()
        {
            Codigo = " gn-01 ",
            Proveedor = new() { Codigo = "PRV-1", RazonSocial = "Proveedor de prueba", Activo = true }
        });
        var sucursal = await context.Sucursal.AsNoTracking().OrderBy(s => s.Id).FirstAsync();
        articulo.Stock.Add(new()
        {
            SucursalId = sucursal.Id, Cantidad = 1.5m
        });
        context.Articulo.Add(articulo);
        await context.SaveChangesAsync();
        // Contexto NUEVO: con el de la siembra, el change tracker devolvería las instancias
        // cargadas aunque la consulta no trajera los códigos ni el stock de la base.
        await using var lectura = fixture.CrearContexto();
        var repo = new ArticuloRepositorio(lectura);
        var detalle = await repo.ObtenerDetallePorIdAsync(articulo.Id);
        Assert.NotNull(detalle);
        Assert.Null(detalle.CostoVigente);
        Assert.Null(detalle.UltimoProveedorId);
        // HU-ART-02: sin filas, lista vacía (no null). La razón de cada ausencia la arma el caso de uso.
        Assert.NotNull(detalle.PreciosPorModalidad);
        Assert.Empty(detalle.PreciosPorModalidad);
        Assert.Equal(" gn-01 ", Assert.Single(detalle.Codigos).CodigoOriginal);
        Assert.Equal("GN-01", detalle.Codigos[0].ClaveNormalizada);
        Assert.Equal("Proveedor de prueba", detalle.Codigos[0].RazonSocial);
        Assert.Equal(1.5m, Assert.Single(detalle.Existencias).Cantidad);
        Assert.Equal(sucursal.Nombre, detalle.Existencias[0].Sucursal);
        Assert.Null(await repo.ObtenerDetallePorIdAsync(checked(articulo.Id + 1)));
    }

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

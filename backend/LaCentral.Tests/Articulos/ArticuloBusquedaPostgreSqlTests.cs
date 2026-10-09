using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos.Dtos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

[Trait("Proveedor", "PostgreSQL")]
public class ArticuloBusquedaPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ArticuloBusquedaPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task BusquedaReal_ConEsquemaRealEIdsGenerados_RespetaRankingYUnicidadD2()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var articulos = Enumerable.Range(1, 40).Select(n => new Articulo
        {
            CodigoInterno = $"ART-{n:D6}", Nombre = $"Bujía NGK pequeña modelo {n:D2}", Activo = n != 40
        }).ToArray();
        articulos[2].CodigoInterno = " GN-01 ";
        var proveedor = new Proveedor { Codigo = "PRV-PG-01", RazonSocial = "Proveedor Test", Activo = true };
        articulos[0].ArticuloCodigoAlternativo.Add(new() { Codigo = "gn-01", Proveedor = proveedor });
        articulos[1].ArticuloCodigoAlternativo.Add(new() { Codigo = " GN-01 ", Proveedor = proveedor });
        await using (var setup = fixture.CrearContexto())
        {
            setup.Articulo.AddRange(articulos);
            await setup.SaveChangesAsync();
        }
        Assert.All(articulos, a => Assert.True(a.Id > 0));
        Assert.Equal(40, articulos.Select(a => a.Id).Distinct().Count());
        Assert.True(proveedor.Id > 0);
        await using (var context = fixture.CrearContexto())
        {
            var repo = new ArticuloRepositorio(context);
            var pagina = await repo.BuscarAsync(new("gn-01*bujia ngk", TamanoPagina: 3));
            Assert.Equal(39, pagina.Total);
            Assert.Equal(new[] { articulos[2].Id, articulos[0].Id, articulos[1].Id }, pagina.Articulos.Select(a => a.Id));
            Assert.Equal(articulos.Take(3).Select(a => a.Id).Order(), Assert.Single(pagina.Conflictos).ArticuloIds);
            var siguiente = await repo.BuscarAsync(new("bujia pequeña", Pagina: 2, TamanoPagina: 3));
            Assert.Equal(articulos.Skip(3).Take(3).Select(a => a.Id), siguiente.Articulos.Select(a => a.Id));
            Assert.Equal(40, (await repo.BuscarAsync(new("bujia", IncluirInactivos: true))).Total);
            Assert.Empty((await repo.BuscarAsync(new("%_"))).Articulos);
            Assert.Empty((await repo.BuscarAsync(new("no existe"))).Articulos);

            var codigos = await context.ArticuloCodigoAlternativo.AsNoTracking().OrderBy(c => c.Codigo).ToListAsync();
            Assert.All(codigos, c => Assert.True(c.Id > 0));
            Assert.All(codigos, c => Assert.Equal(proveedor.Id, c.ProveedorId));
            Assert.Equal(articulos.Take(2).Select(a => a.Id).Order(), codigos.Select(c => c.ArticuloId).Order());
        }
        await using var restriccion = fixture.CrearContexto();
        // Todos los Id de FK provienen de SaveChanges, no de la posición del fixture.
        restriccion.ArticuloCodigoAlternativo.Add(new()
        {
            ArticuloId = articulos[0].Id, ProveedorId = proveedor.Id, Codigo = "OTRO-CODIGO"
        });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => restriccion.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }
}

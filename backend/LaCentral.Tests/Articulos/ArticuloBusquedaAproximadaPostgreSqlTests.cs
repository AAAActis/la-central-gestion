using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-FCO-06 CA-008: con <c>Aproximada</c>, la búsqueda suma las coincidencias por similitud
/// de pg_trgm sobre el nombre y el código interno, después de las exactas. Sin el flag, la
/// búsqueda de la 012 no cambia.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ArticuloBusquedaAproximadaPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ArticuloBusquedaAproximadaPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static async Task<(int Aceite, int Pastilla, int Codigo)> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var aceite = new Articulo { CodigoInterno = "FIL-0712", Nombre = "Filtro de aceite Mann W712", Activo = true };
        var pastilla = new Articulo { CodigoInterno = "PAS-0100", Nombre = "Pastilla de freno delantera", Activo = true };
        var codigo = new Articulo { CodigoInterno = "BUJ-7788", Nombre = "Bujía iridio", Activo = true };
        await using var contexto = fixture.CrearContexto();
        contexto.Articulo.AddRange(aceite, pastilla, codigo);
        await contexto.SaveChangesAsync();
        return (aceite.Id, pastilla.Id, codigo.Id);
    }

    [PostgreSqlFact]
    public async Task Aproximada_NombreConUnErrorDeTipeo_LoEncuentra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var ids = await SembrarAsync(fixture);
        await using var contexto = fixture.CrearContexto();
        var repositorio = new ArticuloRepositorio(contexto);

        var aproximada = await repositorio.BuscarAsync(new("filtro aseite", Aproximada: true));
        var exacta = await repositorio.BuscarAsync(new("filtro aseite"));

        Assert.Contains(aproximada.Articulos, a => a.Id == ids.Aceite);
        Assert.DoesNotContain(aproximada.Articulos, a => a.Id == ids.Pastilla);
        Assert.Empty(exacta.Articulos);
    }

    [PostgreSqlFact]
    public async Task Aproximada_CodigoParecido_LoEncuentra_YElExactoVaPrimero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var ids = await SembrarAsync(fixture);
        await using var contexto = fixture.CrearContexto();
        var repositorio = new ArticuloRepositorio(contexto);

        var parecido = await repositorio.BuscarAsync(new("BUJ-7789", Aproximada: true));
        var exacto = await repositorio.BuscarAsync(new("BUJ-7788", Aproximada: true));

        Assert.Equal(ids.Codigo, Assert.Single(parecido.Articulos).Id);
        Assert.Equal(ids.Codigo, exacto.Articulos[0].Id);
        Assert.Equal(1, exacto.Total);
    }

    // CA-008 (ronda 1 del verificador, M18): lo exacto va antes que lo aproximado, aunque lo
    // aproximado ordene primero por nombre.
    [PostgreSqlFact]
    public async Task Aproximada_LoExactoVaAntesQueLoParecido()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var ids = await SembrarAsync(fixture);
        int parecido;
        await using (var contexto = fixture.CrearContexto())
        {
            var articulo = new Articulo { CodigoInterno = "BUJ-7789", Nombre = "Aaa bujía de otra marca", Activo = true };
            contexto.Articulo.Add(articulo);
            await contexto.SaveChangesAsync();
            parecido = articulo.Id;
        }
        await using var lectura = fixture.CrearContexto();

        var pagina = await new ArticuloRepositorio(lectura).BuscarAsync(new("BUJ-7788", Aproximada: true));

        Assert.Equal(new[] { ids.Codigo, parecido }, pagina.Articulos.Select(a => a.Id));
    }
}

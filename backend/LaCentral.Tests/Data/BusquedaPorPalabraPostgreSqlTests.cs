using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

/// <summary>
/// Fix D (Javi, 21/11): proveedores y clientes encuentran la razón social que contiene cada palabra escrita,
/// sin importar acentos, mayúsculas ni orden, como artículos. Lo parecido (pg_trgm) sigue apareciendo, después.
/// Antes, «Frenos» no encontraba «Frenos y Embragues Córdoba SRL»: la similitud contra el nombre entero daba 0,19.
/// </summary>
[Trait("Base", "PostgreSQL")]
public class BusquedaPorPalabraPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public BusquedaPorPalabraPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    // Palabras inventadas: ningún dato copiado de public las contiene.
    private const string Contiene = "Frenos Wqyz y Embragues Córdoba SRL";
    private const string Lejano = "Distribuidora Mayorista Xvbnk del Sur SA";
    private const string Parecido = "Xvbnq";
    // Una palabra acentuada por vocal, sueltas en un nombre largo: el trigrama no las rescata, solo el filtro sin acentos.
    private const string Acentos = "Comercial Xjqá Bvtké Plní Rztó Mqvú Gwhü del Litoral Norte SA";
    private static readonly string[] SinAcento = { "xjqa", "bvtke", "plni", "rzto", "mqvu", "gwhu" };

    [PostgreSqlFact]
    public Task Proveedores_PorRazonSocialOCodigo_EncuentranPorPalabraContenida_YLoContenidoVaAntesQueLoParecido()
        => Proveedores(porCuit: false);

    [PostgreSqlFact]
    public Task Proveedores_PorRazonSocialOCuit_EncuentranPorPalabraContenida_YLoContenidoVaAntesQueLoParecido()
        => Proveedores(porCuit: true);

    private async Task Proveedores(bool porCuit)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var setup = fixture.CrearContexto())
        {
            setup.Proveedor.AddRange(
                new Proveedor { Codigo = "PAL-1", RazonSocial = Contiene, Activo = true },
                new Proveedor { Codigo = "PAL-2", RazonSocial = Parecido, Activo = true },
                new Proveedor { Codigo = "PAL-3", RazonSocial = Lejano, Activo = true },
                new Proveedor { Codigo = "PAL-4", RazonSocial = Acentos, Activo = true });
            await setup.SaveChangesAsync();
        }

        async Task<string[]> Buscar(string texto)
        {
            await using var contexto = fixture.CrearContexto();
            var (items, _) = await new ProveedorRepositorio(contexto).BuscarAsync(texto, false, 1, 20, porCuit);
            return items.Select(p => p.RazonSocial).ToArray();
        }

        Assert.Contains(Contiene, await Buscar("wqyz"));
        Assert.Contains(Contiene, await Buscar("córdoba WQYZ"));
        Assert.Contains(Contiene, await Buscar("cordoba wqyz"));
        Assert.DoesNotContain(Contiene, await Buscar("wqyz norte"));
        Assert.Equal(new[] { Lejano, Parecido }, await Buscar("Xvbnk"));
        foreach (var palabra in SinAcento)
            Assert.True((await Buscar(palabra)).Contains(Acentos), $"«{palabra}» no encontró «{Acentos}».");
    }

    [PostgreSqlFact]
    public async Task Clientes_EncuentranPorPalabraContenida_YLoContenidoVaAntesQueLoParecido()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var setup = fixture.CrearContexto())
        {
            setup.Cliente.AddRange(
                new Cliente { Codigo = "PAL-1", RazonSocial = Contiene, Activo = true },
                new Cliente { Codigo = "PAL-2", RazonSocial = Parecido, Activo = true },
                new Cliente { Codigo = "PAL-3", RazonSocial = Lejano, Activo = true },
                new Cliente { Codigo = "PAL-4", RazonSocial = Acentos, Activo = true });
            await setup.SaveChangesAsync();
        }

        async Task<string[]> Buscar(string texto)
        {
            await using var contexto = fixture.CrearContexto();
            var (items, _) = await new ClienteRepositorio(contexto).BuscarAsync(texto, false, 1, 20);
            return items.Select(c => c.RazonSocial).ToArray();
        }

        Assert.Contains(Contiene, await Buscar("wqyz"));
        Assert.Contains(Contiene, await Buscar("córdoba WQYZ"));
        Assert.Contains(Contiene, await Buscar("cordoba wqyz"));
        Assert.DoesNotContain(Contiene, await Buscar("wqyz norte"));
        Assert.Equal(new[] { Lejano, Parecido }, await Buscar("Xvbnk"));
        foreach (var palabra in SinAcento)
            Assert.True((await Buscar(palabra)).Contains(Acentos), $"«{palabra}» no encontró «{Acentos}».");
    }
}

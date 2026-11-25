using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// Fix E2: la consulta de razón social EXACTA contra PostgreSQL real, releyendo con un contexto nuevo.
/// Los nombres son inventados con la forma del caso real: uno corto y otro con «S.A.».
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ProveedorRazonSocialExactaPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorRazonSocialExactaPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static Proveedor Prov(string codigo, string razon, bool activo = true) => new()
    {
        Codigo = codigo, RazonSocial = razon, Activo = activo,
        MotivoBaja = activo ? null : "Cierre", FechaBaja = activo ? null : DateTime.UtcNow,
    };

    [PostgreSqlFact]
    public async Task Exacta_NoEsContiene_ElNombreCortoNoTraeAlDeLaSociedad_NiAlReves()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int corto, sociedad;
        await using (var c = fixture.CrearContexto())
        {
            var a = Prov("PRV-EX1", "Zorzal Test");
            var b = Prov("PRV-EX2", "Zorzal Test S.A.");
            var otro = Prov("PRV-EX3", "Nestor Zorzal Test S.A.");
            c.Proveedor.AddRange(a, b, otro);
            await c.SaveChangesAsync();
            (corto, sociedad) = (a.Id, b.Id);
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new ProveedorRepositorio(nuevo);

        Assert.Equal([corto], (await repo.BuscarActivosPorRazonSocialExactaAsync("Zorzal Test", 5)).Select(p => p.Id));
        Assert.Equal([sociedad], (await repo.BuscarActivosPorRazonSocialExactaAsync("ZORZAL TEST S.A.", 5)).Select(p => p.Id));
        Assert.Empty(await repo.BuscarActivosPorRazonSocialExactaAsync("Zorzal", 5));
    }

    [PostgreSqlFact]
    public async Task Exacta_SinNingunaExacta_DevuelveVacio_AunqueHayaQuienLaContenga()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var c = fixture.CrearContexto())
        {
            c.Proveedor.AddRange(Prov("PRV-EX0", "Ninguna Exacta Test S.A."), Prov("PRV-EXA", "Otra Distinta Test"));
            await c.SaveChangesAsync();
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new ProveedorRepositorio(nuevo);

        Assert.Empty(await repo.BuscarActivosPorRazonSocialExactaAsync("Ninguna Exacta Test", 5));
        Assert.Empty(await repo.BuscarActivosPorRazonSocialExactaAsync("No Existe En Absoluto", 5));
    }

    [PostgreSqlFact]
    public async Task Exacta_IgnoraMayusculasAcentosYEspaciosSobrantesORepetidos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int esperado;
        await using (var c = fixture.CrearContexto())
        {
            var a = Prov("PRV-EX4", "  Cordóba   Úrsula  Test ");
            c.Proveedor.Add(a);
            await c.SaveChangesAsync();
            esperado = a.Id;
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new ProveedorRepositorio(nuevo);

        Assert.Equal([esperado], (await repo.BuscarActivosPorRazonSocialExactaAsync("CORDOBA URSULA TEST", 5)).Select(p => p.Id));
        Assert.Equal([esperado], (await repo.BuscarActivosPorRazonSocialExactaAsync("cordóba  úrsula test", 5)).Select(p => p.Id));
    }

    [PostgreSqlFact]
    public async Task Exacta_SoloActivos_EnOrdenDeId_ConTope()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int primero, segundo;
        await using (var c = fixture.CrearContexto())
        {
            var uno = Prov("PRV-EX5", "Duplicado Test");
            var dos = Prov("PRV-EX6", "DUPLICADO TEST");
            var baja = Prov("PRV-EX7", "Duplicado Test", activo: false);
            c.Proveedor.AddRange(uno, dos, baja);
            await c.SaveChangesAsync();
            (primero, segundo) = (uno.Id, dos.Id);
        }
        // Tocar la primera fila la deja físicamente después: sin ORDER BY por Id el orden de lectura cambia.
        await using (var c = fixture.CrearContexto())
        {
            var fila = await c.Proveedor.FindAsync(primero);
            fila!.RazonSocial += " ";
            await c.SaveChangesAsync();
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new ProveedorRepositorio(nuevo);

        Assert.Equal([primero, segundo], (await repo.BuscarActivosPorRazonSocialExactaAsync("duplicado test", 10)).Select(p => p.Id));
        Assert.Equal([primero], (await repo.BuscarActivosPorRazonSocialExactaAsync("duplicado test", 1)).Select(p => p.Id));
    }

    [PostgreSqlFact]
    public async Task Exacta_UnProveedorInactivoNoCuenta()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int activo;
        await using (var c = fixture.CrearContexto())
        {
            var baja = Prov("PRV-EX8", "Solo Baja Test", activo: false);
            var vivo = Prov("PRV-EX9", "SOLO BAJA TEST");
            c.Proveedor.AddRange(baja, vivo);
            await c.SaveChangesAsync();
            activo = vivo.Id;
        }

        await using var nuevo = fixture.CrearContexto();
        Assert.Equal([activo], (await new ProveedorRepositorio(nuevo).BuscarActivosPorRazonSocialExactaAsync("Solo Baja Test", 5)).Select(p => p.Id));
    }
}

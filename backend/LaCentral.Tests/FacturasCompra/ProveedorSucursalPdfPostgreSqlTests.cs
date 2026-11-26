using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// Fix E: las dos consultas nuevas del reconocimiento por PDF, contra PostgreSQL real y releyendo con un
/// contexto nuevo: proveedores activos por razón social y sucursal por código.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ProveedorSucursalPdfPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorSucursalPdfPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task RazonSocial_SoloActivos_SinAcentosNiMayusculas_EnOrdenDeId_ConTope()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int primero, segundo;
        await using (var c = fixture.CrearContexto())
        {
            var activoUno = new Proveedor { Codigo = "PRV-RS1", RazonSocial = "Frenos y Embragues CÓRDOBA SRL", Activo = true };
            var activoDos = new Proveedor { Codigo = "PRV-RS2", RazonSocial = "Córdoba Repuestos SA", Activo = true };
            var inactivo = new Proveedor
            {
                Codigo = "PRV-RS3", RazonSocial = "Cordoba Baja SA", Activo = false,
                MotivoBaja = "Cierre", FechaBaja = DateTime.UtcNow,
            };
            var otro = new Proveedor { Codigo = "PRV-RS4", RazonSocial = "Otro Distinto SA", Activo = true };
            c.Proveedor.AddRange(activoUno, activoDos, inactivo, otro);
            await c.SaveChangesAsync();
            (primero, segundo) = (activoUno.Id, activoDos.Id);
        }
        // Tocar la primera fila la deja físicamente después de la segunda: sin ORDER BY por Id, el orden de
        // lectura ya no coincide con el de los ids.
        await using (var c = fixture.CrearContexto())
        {
            var fila = await c.Proveedor.FindAsync(primero);
            fila!.RazonSocial += " ";
            await c.SaveChangesAsync();
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new ProveedorRepositorio(nuevo);

        var todos = await repo.BuscarActivosEnNegroPorRazonSocialAsync("cordoba", 10);
        Assert.Equal([primero, segundo], todos.Select(p => p.Id));
        Assert.All(todos, p => Assert.True(p.Activo));

        var conTope = await repo.BuscarActivosEnNegroPorRazonSocialAsync("cordoba", 1);
        Assert.Equal([primero], conTope.Select(p => p.Id));

        Assert.Empty(await repo.BuscarActivosEnNegroPorRazonSocialAsync("inexistente", 10));
    }

    [PostgreSqlFact]
    public async Task SucursalPorCodigo_LaDelCodigo_NuncaElDepositoReclamos_NiUnaInexistente()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        short esperada;
        await using (var c = fixture.CrearContexto())
        {
            var sucursal = new Sucursal { Codigo = "TX", Nombre = "Sucursal de prueba", EsReclamos = false };
            // El índice único parcial admite a lo sumo un depósito Reclamos: se toma el que ya hay, o se crea uno.
            var reclamos = c.Sucursal.FirstOrDefault(s => s.EsReclamos) ?? new Sucursal { Codigo = "TR", Nombre = "Reclamos de prueba", EsReclamos = true };
            if (reclamos.Id == 0)
                c.Sucursal.Add(reclamos);
            c.Sucursal.Add(sucursal);
            await c.SaveChangesAsync();
            esperada = sucursal.Id;
            ReclamosCodigo = reclamos.Codigo;
        }

        await using var nuevo = fixture.CrearContexto();
        var repo = new FacturaCompraRepositorio(nuevo);

        Assert.Equal(esperada, await repo.ObtenerSucursalIdPorCodigoAsync("TX"));
        Assert.Null(await repo.ObtenerSucursalIdPorCodigoAsync(ReclamosCodigo!));
        Assert.Null(await repo.ObtenerSucursalIdPorCodigoAsync("NO-EXISTE"));
    }

    private string? ReclamosCodigo;
}

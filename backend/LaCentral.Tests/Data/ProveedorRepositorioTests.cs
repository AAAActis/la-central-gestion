using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

[Trait("Proveedor", "PostgreSQL")]
public class ProveedorRepositorioTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorRepositorioTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task DosCiclosYModificacion_ConservanEstadoEHistorialConIdsGenerados_PostgreSQL()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedor = new Proveedor { Codigo = "PRV-PG-01", RazonSocial = "Proveedor Test", Activo = true };
        await using (var setup = fixture.CrearContexto())
        {
            setup.Proveedor.Add(proveedor);
            await setup.SaveChangesAsync();
        }
        Assert.True(proveedor.Id > 0);
        await using (var baja = fixture.CrearContexto())
            Assert.True((await new DarDeBajaProveedorUseCase(new ProveedorRepositorio(baja)).EjecutarAsync(
                proveedor.Id, new DarDeBajaProveedorRequest("Primera baja", proveedor.Codigo))).IsSuccess);
        ProveedorHistorialBaja primera;
        await using (var leer = fixture.CrearContexto())
        {
            var fila = await leer.Proveedor.AsNoTracking().SingleAsync(p => p.Id == proveedor.Id);
            primera = await leer.ProveedorHistorialBaja.AsNoTracking().SingleAsync(h => h.ProveedorId == proveedor.Id);
            Assert.False(fila.Activo);
            Assert.Equal("Primera baja", fila.MotivoBaja);
            Assert.Equal(fila.FechaBaja, primera.FechaBaja);
            Assert.Equal("Primera baja", primera.Motivo);
            Assert.Null(primera.FechaReactivacion);
        }
        await using (var reactivar = fixture.CrearContexto())
            Assert.True((await new ReactivarProveedorUseCase(new ProveedorRepositorio(reactivar))
                .EjecutarAsync(proveedor.Id)).IsSuccess);
        DateTime? reactivacion;
        await using (var modificar = fixture.CrearContexto())
        {
            var repo = new ProveedorRepositorio(modificar);
            var detalle = (await repo.ObtenerDetallePorIdAsync(proveedor.Id))!;
            Assert.True(detalle.Activo);
            Assert.Equal("Primera baja", detalle.MotivoBaja);
            detalle.RazonSocial = "Proveedor Modificado";
            await repo.ActualizarAsync(detalle);
            var fila = await modificar.Proveedor.AsNoTracking().SingleAsync(p => p.Id == proveedor.Id);
            Assert.True(fila.Activo);
            Assert.Equal(primera.FechaBaja, fila.FechaBaja);
            var cerrada = await modificar.ProveedorHistorialBaja.AsNoTracking().SingleAsync(h => h.Id == primera.Id);
            reactivacion = cerrada.FechaReactivacion;
            Assert.NotNull(reactivacion);
            Assert.Single(await modificar.ProveedorHistorialBaja.ToListAsync());
            await modificar.Proveedor.Include(p => p.HistorialesBaja).SingleAsync(p => p.Id == proveedor.Id);
            Assert.True((await new DarDeBajaProveedorUseCase(repo).EjecutarAsync(proveedor.Id,
                new DarDeBajaProveedorRequest("Segunda baja", proveedor.Codigo))).IsSuccess);
        }
        await using (var final = fixture.CrearContexto())
        {
            var fila = await final.Proveedor.AsNoTracking().SingleAsync(p => p.Id == proveedor.Id);
            var historial = await final.ProveedorHistorialBaja.AsNoTracking()
                .Where(h => h.ProveedorId == proveedor.Id).OrderBy(h => h.Id).ToListAsync();
            Assert.False(fila.Activo);
            Assert.Equal("Proveedor Modificado", fila.RazonSocial);
            Assert.Equal(2, historial.Count);
            Assert.Equal(primera.Id, historial[0].Id);
            Assert.Equal("Primera baja", historial[0].Motivo);
            Assert.Equal(primera.FechaBaja, historial[0].FechaBaja);
            Assert.Equal(reactivacion, historial[0].FechaReactivacion);
            Assert.Equal("Segunda baja", historial[1].Motivo);
            Assert.Equal(fila.FechaBaja, historial[1].FechaBaja);
            Assert.Null(historial[1].FechaReactivacion);
            Assert.NotEqual(historial[0].Id, historial[1].Id);
        }
        await using var restriccion = fixture.CrearContexto();
        restriccion.ProveedorHistorialBaja.Add(new()
            { ProveedorId = proveedor.Id, Motivo = "Duplicada abierta", FechaBaja = DateTime.UtcNow });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => restriccion.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }
}

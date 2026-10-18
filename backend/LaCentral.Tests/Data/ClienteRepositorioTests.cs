using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

[Trait("Proveedor", "PostgreSQL")]
public class ClienteRepositorioTests
{
    private readonly ITestOutputHelper _salida;
    public ClienteRepositorioTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task DosCiclos_ConservanEstadoEHistorialConIdsGenerados_PostgreSQL()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var cliente = new Cliente { Codigo = "CLI-PG-01", RazonSocial = "Cliente Test", Activo = true };
        await using (var setup = fixture.CrearContexto())
        {
            setup.Cliente.Add(cliente);
            await setup.SaveChangesAsync();
        }
        Assert.True(cliente.Id > 0);
        await using (var baja = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaCliente(baja, usuarioId)
                .EjecutarAsync(cliente.Id, cliente.Codigo, "Primera baja")).IsSuccess);
        ClienteHistorialBaja primera;
        await using (var leer = fixture.CrearContexto())
        {
            var fila = await leer.Cliente.AsNoTracking().SingleAsync(c => c.Id == cliente.Id);
            primera = await leer.ClienteHistorialBaja.AsNoTracking().SingleAsync(h => h.ClienteId == cliente.Id);
            Assert.False(fila.Activo);
            Assert.Equal("Primera baja", fila.MotivoBaja);
            Assert.Equal(fila.FechaBaja, primera.FechaBaja);
            Assert.Equal("Primera baja", primera.Motivo);
            Assert.Null(primera.FechaReactivacion);
        }
        await using (var reactivar = fixture.CrearContexto())
        {
            var repo = new ClienteRepositorio(reactivar);
            var detalle = (await repo.ObtenerDetallePorIdAsync(cliente.Id))!;
            Assert.Equal(primera.FechaBaja, detalle.FechaBaja);
            Assert.Equal(primera.Motivo, detalle.MotivoBaja);
            Assert.True((await CasosDeBajaPostgreSql.ReactivarCliente(reactivar, usuarioId).EjecutarAsync(cliente.Id)).IsSuccess);
        }
        DateTime? reactivacion;
        await using (var modificar = fixture.CrearContexto())
        {
            var repo = new ClienteRepositorio(modificar);
            var detalle = (await repo.ObtenerDetallePorIdAsync(cliente.Id))!;
            Assert.True(detalle.Activo);
            Assert.Equal(primera.FechaBaja, detalle.FechaBaja);
            Assert.Equal(primera.Motivo, detalle.MotivoBaja);
            detalle.RazonSocial = "Cliente Modificado";
            await repo.ActualizarAsync(detalle);
            var cerrada = await modificar.ClienteHistorialBaja.AsNoTracking().SingleAsync(h => h.Id == primera.Id);
            reactivacion = cerrada.FechaReactivacion;
            Assert.NotNull(reactivacion);
            Assert.Single(await modificar.ClienteHistorialBaja.ToListAsync());
            // Dejar ambas bajas tracked detecta la inferencia 1:1 incorrecta.
            await modificar.Cliente.Include(c => c.HistorialesBaja).SingleAsync(c => c.Id == cliente.Id);
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaCliente(modificar, usuarioId)
                .EjecutarAsync(cliente.Id, cliente.Codigo, "Segunda baja")).IsSuccess);
        }
        await using (var final = fixture.CrearContexto())
        {
            var fila = await final.Cliente.AsNoTracking().SingleAsync(c => c.Id == cliente.Id);
            var historial = await final.ClienteHistorialBaja.AsNoTracking()
                .Where(h => h.ClienteId == cliente.Id).OrderBy(h => h.Id).ToListAsync();
            Assert.False(fila.Activo);
            Assert.Equal("Cliente Modificado", fila.RazonSocial);
            Assert.Equal(2, historial.Count);
            Assert.Equal(primera.Id, historial[0].Id);
            Assert.Equal(primera.FechaBaja, historial[0].FechaBaja);
            Assert.Equal("Primera baja", historial[0].Motivo);
            Assert.Equal(reactivacion, historial[0].FechaReactivacion);
            Assert.Equal("Segunda baja", historial[1].Motivo);
            Assert.Equal(fila.FechaBaja, historial[1].FechaBaja);
            Assert.Null(historial[1].FechaReactivacion);
            Assert.NotEqual(historial[0].Id, historial[1].Id);
        }
        await using var restriccion = fixture.CrearContexto();
        restriccion.ClienteHistorialBaja.Add(new()
            { ClienteId = cliente.Id, Motivo = "Duplicada abierta", FechaBaja = DateTime.UtcNow });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => restriccion.SaveChangesAsync());
        Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
    }

    // 1.9: una dirección importada sin calle viaja como "" (convención del dominio) y
    // el ciclo baja → reactivación no la duplica ni la borra.
    [PostgreSqlFact]
    public async Task DireccionSinCalle_SeLeeComoVaciaYNoSeDuplicaAlActualizar_PostgreSQL()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var cliente = new Cliente { Codigo = "CLI-DIR", RazonSocial = "Cliente Sin Calle", Activo = true };
        cliente.ClienteDireccion.Add(new ClienteDireccion { Calle = null, Localidad = "Córdoba" });
        await using (var setup = fixture.CrearContexto())
        {
            setup.Cliente.Add(cliente);
            await setup.SaveChangesAsync();
        }

        await using (var leer = fixture.CrearContexto())
            Assert.Equal(new[] { string.Empty }, (await new ClienteRepositorio(leer).ObtenerDetallePorIdAsync(cliente.Id))!.Direcciones);
        await using (var baja = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaCliente(baja, usuarioId)
                .EjecutarAsync(cliente.Id, cliente.Codigo, "Baja con dirección sin calle")).IsSuccess);
        await using (var reactivar = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.ReactivarCliente(reactivar, usuarioId).EjecutarAsync(cliente.Id)).IsSuccess);

        await using var final = fixture.CrearContexto();
        var direccion = Assert.Single(await final.ClienteDireccion.AsNoTracking().Where(d => d.ClienteId == cliente.Id).ToListAsync());
        Assert.Null(direccion.Calle);
        Assert.Equal("Córdoba", direccion.Localidad);
    }
}

using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Npgsql;
using Xunit.Abstractions;
using ClienteBd = LaCentral.Data.Models.Cliente;
using ProveedorBd = LaCentral.Data.Models.Proveedor;
using ArticuloBd = LaCentral.Data.Models.Articulo;
using UsuarioBd = LaCentral.Data.Models.Usuario;

namespace LaCentral.Tests.Data;

/// <summary>
/// D10 contra PostgreSQL real: el puerto de historial en las cuatro tablas, el índice
/// único parcial y la atomicidad de la baja con su historial. Todo se relee con un
/// contexto nuevo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class HistorialBajaRepositorioTests
{
    private static readonly EntidadConHistorialBaja[] Entidades = Enum.GetValues<EntidadConHistorialBaja>();
    private static readonly DateTime PrimeraBaja = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Reactivacion = new(2026, 4, 1, 11, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime SegundaBaja = new(2026, 5, 1, 9, 15, 0, DateTimeKind.Utc);

    private readonly ITestOutputHelper _salida;
    public HistorialBajaRepositorioTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task BajaReactivacionBaja_DejaDosRegistrosConMotivosFechasYUsuarios_EnLasCuatroEntidades()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync("da.de.baja");
        var quienReactiva = await fixture.CrearUsuarioAsync("reactiva");

        foreach (var entidad in Entidades)
        {
            var id = await SembrarAsync(fixture, entidad);

            await using (var contexto = fixture.CrearContexto())
                await new HistorialBajaRepositorio(contexto).AbrirAsync(entidad, id, "Primera baja", PrimeraBaja, quienDaDeBaja);
            await using (var contexto = fixture.CrearContexto())
                Assert.True(await new HistorialBajaRepositorio(contexto).CerrarAsync(entidad, id, Reactivacion, quienReactiva));
            await using (var contexto = fixture.CrearContexto())
                await new HistorialBajaRepositorio(contexto).AbrirAsync(entidad, id, "Segunda baja", SegundaBaja, quienDaDeBaja);

            await using var lectura = fixture.CrearContexto();
            var historial = await new HistorialBajaRepositorio(lectura).ObtenerAsync(entidad, id);
            Assert.Equal(2, historial.Count);
            Assert.Equal(2, await ContarFilasAsync(lectura, entidad, id));

            // La reactivación cierra la baja y la fila conserva motivo y fecha (HU-CLI-05 CA-003).
            Assert.Equal("Primera baja", historial[0].Motivo);
            Assert.Equal(PrimeraBaja, historial[0].FechaBaja);
            Assert.Equal(Reactivacion, historial[0].FechaReactivacion);
            Assert.Equal(quienDaDeBaja, historial[0].UsuarioBajaId);
            Assert.Equal(quienReactiva, historial[0].UsuarioReactivacionId);

            Assert.Equal("Segunda baja", historial[1].Motivo);
            Assert.Equal(SegundaBaja, historial[1].FechaBaja);
            Assert.Null(historial[1].FechaReactivacion);
            Assert.Equal(quienDaDeBaja, historial[1].UsuarioBajaId);
            Assert.Null(historial[1].UsuarioReactivacionId);
            _salida.WriteLine($"{entidad}: dos registros, uno cerrado y uno abierto.");
        }
    }

    [PostgreSqlFact]
    public async Task DosBajasAbiertasSimultaneas_LaSegundaDa23505EnElIndiceParcial_EnLasCuatroEntidades()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();

        foreach (var entidad in Entidades)
        {
            var id = await SembrarAsync(fixture, entidad);
            await using var primero = fixture.CrearContexto();
            await using var segundo = fixture.CrearContexto();

            var intentos = new[]
            {
                Intentar(() => new HistorialBajaRepositorio(primero).AbrirAsync(entidad, id, "Baja A", PrimeraBaja, usuarioId)),
                Intentar(() => new HistorialBajaRepositorio(segundo).AbrirAsync(entidad, id, "Baja B", SegundaBaja, usuarioId))
            };
            var errores = (await Task.WhenAll(intentos)).OfType<Exception>().ToList();

            var error = Assert.IsType<DbUpdateException>(Assert.Single(errores));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, Assert.IsType<PostgresException>(error.InnerException).SqlState);
            await using var lectura = fixture.CrearContexto();
            Assert.Equal(1, await ContarFilasAsync(lectura, entidad, id));
        }
    }

    [PostgreSqlFact]
    public async Task CerrarSinBajaAbierta_DevuelveFalseYNoEscribe()
    {
        // Los inactivos importados de Multisoft no tienen historial: reactivarlos no debe fallar.
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var id = await SembrarAsync(fixture, EntidadConHistorialBaja.Cliente);

        await using (var contexto = fixture.CrearContexto())
            Assert.False(await new HistorialBajaRepositorio(contexto).CerrarAsync(EntidadConHistorialBaja.Cliente, id, Reactivacion, usuarioId));

        await using var lectura = fixture.CrearContexto();
        Assert.Equal(0, await ContarFilasAsync(lectura, EntidadConHistorialBaja.Cliente, id));
    }

    [PostgreSqlFact]
    public async Task CasosDeUso_UsuariosDeBajaYReactivacionSalenDeLaSesion_YClientePersisteUsuarioBajaId()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync("da.de.baja");
        var quienReactiva = await fixture.CrearUsuarioAsync("reactiva");
        var clienteId = await SembrarAsync(fixture, EntidadConHistorialBaja.Cliente);
        var proveedorId = await SembrarAsync(fixture, EntidadConHistorialBaja.Proveedor);
        var usuarioId = await SembrarAsync(fixture, EntidadConHistorialBaja.Usuario);

        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaCliente(c, quienDaDeBaja).EjecutarAsync(clienteId, $"CLI-{clienteId}", "Mudanza")).IsSuccess);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaProveedor(c, quienDaDeBaja)
                .EjecutarAsync(proveedorId, new DarDeBajaProveedorRequest("Mudanza", $"PRV-{proveedorId}"))).IsSuccess);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaUsuario(c, quienDaDeBaja).EjecutarAsync(usuarioId, "Mudanza")).IsSuccess);

        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.ReactivarCliente(c, quienReactiva).EjecutarAsync(clienteId)).IsSuccess);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.ReactivarProveedor(c, quienReactiva).EjecutarAsync(proveedorId)).IsSuccess);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.ReactivarUsuario(c, quienReactiva).EjecutarAsync(usuarioId)).IsSuccess);

        await using var lectura = fixture.CrearContexto();
        var cliente = await lectura.Cliente.AsNoTracking().SingleAsync(x => x.Id == clienteId);
        Assert.True(cliente.Activo);
        Assert.Equal("Mudanza", cliente.MotivoBaja);
        Assert.Equal(quienDaDeBaja, cliente.UsuarioBajaId);

        var historialCliente = Assert.Single(await new HistorialBajaRepositorio(lectura).ObtenerAsync(EntidadConHistorialBaja.Cliente, clienteId));
        Assert.Equal(cliente.FechaBaja, historialCliente.FechaBaja);
        foreach (var (entidad, id) in new[] { (EntidadConHistorialBaja.Cliente, clienteId),
            (EntidadConHistorialBaja.Proveedor, proveedorId), (EntidadConHistorialBaja.Usuario, usuarioId) })
        {
            Assert.Equal(1, await ContarFilasAsync(lectura, entidad, id));
            var h = Assert.Single(await new HistorialBajaRepositorio(lectura).ObtenerAsync(entidad, id));
            Assert.Equal("Mudanza", h.Motivo);
            Assert.True(h.FechaReactivacion > h.FechaBaja);
            Assert.Equal(quienDaDeBaja, h.UsuarioBajaId);
            Assert.Equal(quienReactiva, h.UsuarioReactivacionId);
        }
    }

    // ---------- Atomicidad: la baja y su historial, juntos o ninguno ----------

    [PostgreSqlFact]
    public async Task DarDeBaja_FallaAlAbrirHistorial_LaFilaNoQuedaInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var clienteId = await SembrarAsync(fixture, EntidadConHistorialBaja.Cliente);
        // Dato inconsistente: cliente activo con una baja abierta. El historial choca
        // contra el índice parcial DESPUÉS de que la fila ya se actualizó.
        await using (var setup = fixture.CrearContexto())
        {
            setup.ClienteHistorialBaja.Add(new ClienteHistorialBaja { ClienteId = clienteId, Motivo = "Huérfana", FechaBaja = PrimeraBaja });
            await setup.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                CasosDeBajaPostgreSql.DarDeBajaCliente(contexto, usuarioId).EjecutarAsync(clienteId, $"CLI-{clienteId}", "Mudanza"));

        await using var lectura = fixture.CrearContexto();
        var cliente = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == clienteId);
        Assert.True(cliente.Activo);
        Assert.Null(cliente.MotivoBaja);
        Assert.Null(cliente.FechaBaja);
        Assert.Null(cliente.UsuarioBajaId);
        Assert.Equal("Huérfana", (await lectura.ClienteHistorialBaja.AsNoTracking().SingleAsync(h => h.ClienteId == clienteId)).Motivo);
    }

    [PostgreSqlFact]
    public async Task Reactivar_FallaAlCerrarHistorial_LaFilaSigueInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var proveedorId = await SembrarAsync(fixture, EntidadConHistorialBaja.Proveedor);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaProveedor(c, usuarioId)
                .EjecutarAsync(proveedorId, new DarDeBajaProveedorRequest("Mudanza", $"PRV-{proveedorId}"))).IsSuccess);

        var historialQueFalla = new Mock<IHistorialBajaRepositorio>();
        historialQueFalla.Setup(h => h.CerrarAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falla forzada después de actualizar la fila."));
        await using (var contexto = fixture.CrearContexto())
        {
            var caso = new LaCentral.UseCases.Proveedores.ReactivarProveedorUseCase(new ProveedorRepositorio(contexto),
                historialQueFalla.Object, new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(proveedorId));
        }

        await using var lectura = fixture.CrearContexto();
        Assert.False((await lectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == proveedorId)).Activo);
        Assert.Null((await lectura.ProveedorHistorialBaja.AsNoTracking().SingleAsync(h => h.ProveedorId == proveedorId)).FechaReactivacion);
    }

    // Hallazgo 1 del auditor: las otras cuatro operaciones de baja y reactivación.

    [PostgreSqlFact]
    public async Task DarDeBajaProveedor_FallaAlAbrirHistorial_LaFilaNoQuedaInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var proveedorId = await SembrarAsync(fixture, EntidadConHistorialBaja.Proveedor);
        // Baja abierta huérfana: el historial choca contra el índice parcial después de actualizar la fila.
        await using (var setup = fixture.CrearContexto())
        {
            setup.ProveedorHistorialBaja.Add(new ProveedorHistorialBaja { ProveedorId = proveedorId, Motivo = "Huérfana", FechaBaja = PrimeraBaja });
            await setup.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
            await Assert.ThrowsAsync<DbUpdateException>(() => CasosDeBajaPostgreSql.DarDeBajaProveedor(contexto, usuarioId)
                .EjecutarAsync(proveedorId, new DarDeBajaProveedorRequest("Mudanza", $"PRV-{proveedorId}")));

        await using var lectura = fixture.CrearContexto();
        var proveedor = await lectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == proveedorId);
        Assert.True(proveedor.Activo);
        Assert.Null(proveedor.MotivoBaja);
        Assert.Null(proveedor.FechaBaja);
        Assert.Equal("Huérfana", (await lectura.ProveedorHistorialBaja.AsNoTracking().SingleAsync(h => h.ProveedorId == proveedorId)).Motivo);
    }

    [PostgreSqlFact]
    public async Task DarDeBajaUsuario_FallaAlAbrirHistorial_LaFilaNoQuedaInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync();
        var usuarioId = await SembrarAsync(fixture, EntidadConHistorialBaja.Usuario);
        await using (var setup = fixture.CrearContexto())
        {
            setup.UsuarioHistorialBaja.Add(new UsuarioHistorialBaja { UsuarioId = usuarioId, Motivo = "Huérfana", FechaBaja = PrimeraBaja });
            await setup.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                CasosDeBajaPostgreSql.DarDeBajaUsuario(contexto, quienDaDeBaja).EjecutarAsync(usuarioId, "Licencia"));

        await using var lectura = fixture.CrearContexto();
        var usuario = await lectura.Usuario.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        Assert.True(usuario.Activo);
        Assert.Null(usuario.MotivoBaja);
        Assert.Null(usuario.FechaBaja);
        Assert.Null(usuario.UsuarioBajaId);
        Assert.Equal("Huérfana", (await lectura.UsuarioHistorialBaja.AsNoTracking().SingleAsync(h => h.UsuarioId == usuarioId)).Motivo);
    }

    [PostgreSqlFact]
    public async Task ReactivarCliente_FallaAlCerrarHistorial_LaFilaSigueInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var clienteId = await SembrarAsync(fixture, EntidadConHistorialBaja.Cliente);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaCliente(c, usuarioId).EjecutarAsync(clienteId, $"CLI-{clienteId}", "Mudanza")).IsSuccess);

        await using (var contexto = fixture.CrearContexto())
        {
            var caso = new LaCentral.UseCases.ReactivarClienteUseCase(new ClienteRepositorio(contexto),
                HistorialQueFallaAlCerrar().Object, new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(clienteId));
        }

        await using var lectura = fixture.CrearContexto();
        var cliente = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == clienteId);
        Assert.False(cliente.Activo);
        Assert.Equal("Mudanza", cliente.MotivoBaja);
        Assert.Equal(usuarioId, cliente.UsuarioBajaId);
        Assert.Null((await lectura.ClienteHistorialBaja.AsNoTracking().SingleAsync(h => h.ClienteId == clienteId)).FechaReactivacion);
    }

    [PostgreSqlFact]
    public async Task ReactivarUsuario_FallaAlCerrarHistorial_LaFilaSigueInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync();
        var usuarioId = await SembrarAsync(fixture, EntidadConHistorialBaja.Usuario);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaUsuario(c, quienDaDeBaja).EjecutarAsync(usuarioId, "Licencia")).IsSuccess);

        await using (var contexto = fixture.CrearContexto())
        {
            var caso = new LaCentral.UseCases.ReactivarUsuarioUseCase(new UsuarioRepositorio(contexto),
                HistorialQueFallaAlCerrar().Object, new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(quienDaDeBaja).Object);
            await Assert.ThrowsAsync<InvalidOperationException>(() => caso.EjecutarAsync(usuarioId));
        }

        // La reactivación limpia motivo, fecha y usuario de la fila: el rollback los tiene que devolver.
        await using var lectura = fixture.CrearContexto();
        var usuario = await lectura.Usuario.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        Assert.False(usuario.Activo);
        Assert.Equal("Licencia", usuario.MotivoBaja);
        Assert.NotNull(usuario.FechaBaja);
        Assert.Equal(quienDaDeBaja, usuario.UsuarioBajaId);
        Assert.Null((await lectura.UsuarioHistorialBaja.AsNoTracking().SingleAsync(h => h.UsuarioId == usuarioId)).FechaReactivacion);
    }

    // ---------- Hallazgo 3: reactivación anterior a la baja (CHECK *_hist_fechas_coherentes) ----------

    [PostgreSqlFact]
    public async Task ReactivacionAnteriorALaBaja_EnLasCuatroTablas_ElManejadorLaReconoce()
    {
        // Una fecha_baja futura (backfill de Multisoft o reloj desfasado) hace que el CHECK
        // rechace la reactivación con 23514. Sin reconocerla, el manejador devuelve 500.
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var bajaFutura = DateTime.UtcNow.AddDays(30);

        foreach (var entidad in Entidades)
        {
            var id = await SembrarAsync(fixture, entidad);
            await using (var contexto = fixture.CrearContexto())
                await new HistorialBajaRepositorio(contexto).AbrirAsync(entidad, id, "Baja futura", bajaFutura, usuarioId);

            await using (var contexto = fixture.CrearContexto())
            {
                var error = await Assert.ThrowsAnyAsync<Exception>(() =>
                    new HistorialBajaRepositorio(contexto).CerrarAsync(entidad, id, bajaFutura.AddDays(-1), usuarioId));
                _salida.WriteLine($"{entidad}: {error.GetType().Name} {(error as PostgresException ?? error.InnerException as PostgresException)?.ConstraintName}");
                Assert.NotNull(LaCentral.Api.Middleware.ManejadorExcepcionesGlobal.MensajeDeConflicto(error));
            }

            await using var lectura = fixture.CrearContexto();
            Assert.Null(Assert.Single(await new HistorialBajaRepositorio(lectura).ObtenerAsync(entidad, id)).FechaReactivacion);
        }
    }

    [PostgreSqlFact]
    public async Task ReactivarCliente_ConBajaFutura_ElManejadorLaReconoceYLaFilaSigueInactiva()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var clienteId = await SembrarAsync(fixture, EntidadConHistorialBaja.Cliente);
        var bajaFutura = DateTime.UtcNow.AddDays(30);
        await using (var setup = fixture.CrearContexto())
        {
            var fila = await setup.Cliente.SingleAsync(c => c.Id == clienteId);
            fila.Activo = false;
            fila.MotivoBaja = "Baja futura";
            fila.FechaBaja = bajaFutura;
            setup.ClienteHistorialBaja.Add(new ClienteHistorialBaja { ClienteId = clienteId, Motivo = "Baja futura", FechaBaja = bajaFutura, UsuarioBajaId = usuarioId });
            await setup.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
        {
            var error = await Assert.ThrowsAnyAsync<Exception>(() =>
                CasosDeBajaPostgreSql.ReactivarCliente(contexto, usuarioId).EjecutarAsync(clienteId));
            Assert.Equal("La fecha de reactivación no puede ser anterior a la fecha de la baja.",
                LaCentral.Api.Middleware.ManejadorExcepcionesGlobal.MensajeDeConflicto(error));
        }

        await using var lectura = fixture.CrearContexto();
        Assert.False((await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == clienteId)).Activo);
        Assert.Null((await lectura.ClienteHistorialBaja.AsNoTracking().SingleAsync(h => h.ClienteId == clienteId)).FechaReactivacion);
    }

    [PostgreSqlFact]
    public async Task UnidadDeTrabajo_OperacionQueDevuelveFallo_DescartaLoEscrito()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);

        await using (var contexto = fixture.CrearContexto())
        {
            var resultado = await new UnidadDeTrabajo(contexto).EjecutarAsync(async ct =>
            {
                contexto.Cliente.Add(new ClienteBd { Codigo = "CLI-DESCARTADO", RazonSocial = "Descartado", Activo = true });
                await contexto.SaveChangesAsync(ct);
                return Result.Failure(TipoError.Conflicto, "Una regla falló después de escribir.");
            });
            Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        }

        await using var lectura = fixture.CrearContexto();
        Assert.False(await lectura.Cliente.AnyAsync(c => c.Codigo == "CLI-DESCARTADO"));
    }

    // Los casos de uso leen lo que la operación produjo solo si la unidad devolvió éxito
    // (CrearArticuloUseCase, el Id y el código interno). La implementación real no tiene
    // un éxito sin operación: ejecuta la operación una vez y devuelve SU resultado, tanto
    // con la transacción propia como anidada en otra.
    [PostgreSqlFact]
    public async Task UnidadDeTrabajo_DevuelveElResultadoDeLaOperacionYLaEjecutaUnaVez()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var contexto = fixture.CrearContexto();
        var unidad = new UnidadDeTrabajo(contexto);
        var ejecuciones = 0;
        Func<CancellationToken, Task<Result>> Operacion(Result devuelve) => _ =>
        {
            ejecuciones++;
            return Task.FromResult(devuelve);
        };

        var exito = await unidad.EjecutarAsync(Operacion(Result.Success()));
        Assert.True(exito.IsSuccess);
        Assert.Equal(1, ejecuciones);

        var fallo = await unidad.EjecutarAsync(Operacion(Result.Failure(TipoError.Invalido, "falló")));
        Assert.Equal(TipoError.Invalido, fallo.Tipo);
        Assert.Equal("falló", fallo.Error);
        Assert.Equal(2, ejecuciones);

        Result anidadaExito = Result.Failure(TipoError.Conflicto, "sin ejecutar");
        Result anidadaFallo = Result.Success();
        await unidad.EjecutarAsync(async ct =>
        {
            anidadaExito = await unidad.EjecutarAsync(Operacion(Result.Success()), ct);
            anidadaFallo = await unidad.EjecutarAsync(Operacion(Result.Failure(TipoError.NoEncontrado, "anidada")), ct);
            return Result.Success();
        });
        Assert.True(anidadaExito.IsSuccess);
        Assert.Equal(TipoError.NoEncontrado, anidadaFallo.Tipo);
        Assert.Equal(4, ejecuciones);
    }

    // ---------- Apoyo ----------

    private static Mock<IHistorialBajaRepositorio> HistorialQueFallaAlCerrar()
    {
        var historial = new Mock<IHistorialBajaRepositorio>();
        historial.Setup(h => h.CerrarAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(),
                It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falla forzada después de actualizar la fila."));
        return historial;
    }

    private static async Task<Exception?> Intentar(Func<Task> accion)
    {
        try { await accion(); return null; }
        catch (Exception e) { return e; }
    }

    private static async Task<int> SembrarAsync(PostgreSqlPruebas fixture, EntidadConHistorialBaja entidad)
    {
        await using var contexto = fixture.CrearContexto();
        var sufijo = Guid.NewGuid().ToString("N")[..8];
        switch (entidad)
        {
            case EntidadConHistorialBaja.Cliente:
                var cliente = new ClienteBd { Codigo = "TMP", RazonSocial = $"Cliente {sufijo}", Activo = true };
                contexto.Cliente.Add(cliente);
                await contexto.SaveChangesAsync();
                cliente.Codigo = $"CLI-{cliente.Id}";
                await contexto.SaveChangesAsync();
                return cliente.Id;
            case EntidadConHistorialBaja.Proveedor:
                var proveedor = new ProveedorBd { Codigo = "TMP", RazonSocial = $"Proveedor {sufijo}", Activo = true };
                contexto.Proveedor.Add(proveedor);
                await contexto.SaveChangesAsync();
                proveedor.Codigo = $"PRV-{proveedor.Id}";
                await contexto.SaveChangesAsync();
                return proveedor.Id;
            case EntidadConHistorialBaja.Articulo:
                var articulo = new ArticuloBd { CodigoInterno = $"ART-{sufijo}", Nombre = $"Artículo {sufijo}", Activo = true };
                contexto.Articulo.Add(articulo);
                await contexto.SaveChangesAsync();
                return articulo.Id;
            default:
                var usuario = new UsuarioBd
                {
                    NombreUsuario = $"usuario.{sufijo}", HashContrasena = "hash-de-prueba", Activo = true,
                    RolId = await contexto.Rol.OrderBy(r => r.Id).Select(r => r.Id).FirstAsync(),
                    SucursalId = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync()
                };
                contexto.Usuario.Add(usuario);
                await contexto.SaveChangesAsync();
                return usuario.Id;
        }
    }

    private static Task<int> ContarFilasAsync(LaCentralDbContext contexto, EntidadConHistorialBaja entidad, int id) => entidad switch
    {
        EntidadConHistorialBaja.Cliente => contexto.ClienteHistorialBaja.CountAsync(h => h.ClienteId == id),
        EntidadConHistorialBaja.Proveedor => contexto.ProveedorHistorialBaja.CountAsync(h => h.ProveedorId == id),
        EntidadConHistorialBaja.Articulo => contexto.ArticuloHistorialBaja.CountAsync(h => h.ArticuloId == id),
        _ => contexto.UsuarioHistorialBaja.CountAsync(h => h.UsuarioId == id)
    };
}

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

    // ---------- Apoyo ----------

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

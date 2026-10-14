using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

/// <summary>
/// usuario.usuario_baja_id contra PostgreSQL real (hallazgo 2 del auditor, ítem 2.2).
/// Las lecturas del repositorio no lo traían y ActualizarAsync lo escribía en NULL:
/// cualquier actualización de un usuario inactivo borraba quién lo dio de baja.
/// Todo se relee con un contexto nuevo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class UsuarioRepositorioPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public UsuarioRepositorioPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task DarDeBaja_ReleidoConContextoNuevo_TraeElUsuarioQueLaHizo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync("da.de.baja");
        var usuarioId = await fixture.CrearUsuarioAsync("objetivo");

        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaUsuario(c, quienDaDeBaja).EjecutarAsync(usuarioId, "Licencia")).IsSuccess);

        await using (var lectura = fixture.CrearContexto())
            Assert.Equal(quienDaDeBaja, (await lectura.Usuario.AsNoTracking().SingleAsync(u => u.Id == usuarioId)).UsuarioBajaId);
        await using (var lectura = fixture.CrearContexto())
            Assert.Equal(quienDaDeBaja, (await new UsuarioRepositorio(lectura).ObtenerPorIdAsync(usuarioId))!.UsuarioBajaId);
        await using (var lectura = fixture.CrearContexto())
            Assert.Equal(quienDaDeBaja, (await new UsuarioRepositorio(lectura).ObtenerPorNombreAsync("objetivo"))!.UsuarioBajaId);
    }

    [PostgreSqlFact]
    public async Task ActualizarUsuarioInactivo_ConservaElUsuarioQueLoDioDeBaja()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync("da.de.baja");
        var usuarioId = await fixture.CrearUsuarioAsync("objetivo");
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaUsuario(c, quienDaDeBaja).EjecutarAsync(usuarioId, "Licencia")).IsSuccess);

        // Restablecer la contraseña lee el usuario por el repositorio y lo vuelve a escribir.
        var hash = new Mock<IServicioHash>();
        hash.Setup(h => h.HashearClave(It.IsAny<string>())).Returns("hash-nuevo");
        await using (var c = fixture.CrearContexto())
            Assert.True((await new RestablecerContrasenaUseCase(new UsuarioRepositorio(c), hash.Object)
                .EjecutarAsync(usuarioId, "clave-nueva")).IsSuccess);

        await using var lectura = fixture.CrearContexto();
        var fila = await lectura.Usuario.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        Assert.Equal("hash-nuevo", fila.HashContrasena);
        Assert.False(fila.Activo);
        Assert.Equal("Licencia", fila.MotivoBaja);
        Assert.Equal(quienDaDeBaja, fila.UsuarioBajaId);
    }

    // Caracterización: la reactivación de usuario limpia la fila (decisión 2.2) y el
    // rastro queda en el historial. Hasta ahora usuario_baja_id se limpiaba solo por el
    // mapeo que lo perdía; con la lectura corregida, tiene que limpiarlo el caso de uso.
    [PostgreSqlFact]
    public async Task Reactivar_LimpiaLaFila_YElHistorialConservaQuienDioDeBaja()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienDaDeBaja = await fixture.CrearUsuarioAsync("da.de.baja");
        var quienReactiva = await fixture.CrearUsuarioAsync("reactiva");
        var usuarioId = await fixture.CrearUsuarioAsync("objetivo");
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaUsuario(c, quienDaDeBaja).EjecutarAsync(usuarioId, "Licencia")).IsSuccess);
        await using (var c = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.ReactivarUsuario(c, quienReactiva).EjecutarAsync(usuarioId)).IsSuccess);

        await using var lectura = fixture.CrearContexto();
        var fila = await lectura.Usuario.AsNoTracking().SingleAsync(u => u.Id == usuarioId);
        Assert.True(fila.Activo);
        Assert.Null(fila.MotivoBaja);
        Assert.Null(fila.FechaBaja);
        Assert.Null(fila.UsuarioBajaId);
        var historial = Assert.Single(await new HistorialBajaRepositorio(lectura).ObtenerAsync(EntidadConHistorialBaja.Usuario, usuarioId));
        Assert.Equal(quienDaDeBaja, historial.UsuarioBajaId);
        Assert.Equal(quienReactiva, historial.UsuarioReactivacionId);
    }
}

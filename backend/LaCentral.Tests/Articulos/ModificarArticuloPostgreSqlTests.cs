using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-03 contra PostgreSQL real: casos de uso, repositorios y unidad de trabajo reales,
/// todos sobre el mismo contexto (como el alcance scoped de la API). Solo la sesión es un
/// doble. Todo se relee con un contexto nuevo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class ModificarArticuloPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ModificarArticuloPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static IContextoUsuario Sesion(int usuarioId, bool administrador)
    {
        var sesion = DoblesBaja.Contexto(usuarioId);
        sesion.SetupGet(s => s.EsAdministrador).Returns(administrador);
        return sesion.Object;
    }

    private static ModificarArticuloUseCase Modificar(LaCentralDbContext contexto, bool administrador = false,
        IConsultaArticulosRepositorio? consulta = null) =>
        new(new ArticuloRepositorio(contexto), consulta ?? new ArticuloRepositorio(contexto),
            new ProveedorRepositorio(contexto), new UnidadDeTrabajo(contexto), Sesion(1, administrador));

    private static RetirarCodigoProveedorUseCase Retirar(LaCentralDbContext contexto) =>
        new(new ArticuloRepositorio(contexto), new ArticuloRepositorio(contexto), Sesion(1, administrador: true));

    private static DarDeBajaArticuloUseCase Baja(LaCentralDbContext contexto, int usuarioId) =>
        new(new ArticuloRepositorio(contexto), new ArticuloRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), Sesion(usuarioId, administrador: false));

    private static ReactivarArticuloUseCase Reactivar(LaCentralDbContext contexto, int usuarioId) =>
        new(new ArticuloRepositorio(contexto), new ArticuloRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), Sesion(usuarioId, administrador: false));

    private static async Task<int> CrearProveedorAsync(PostgreSqlPruebas fixture, string codigo)
    {
        await using var contexto = fixture.CrearContexto();
        var proveedor = new Proveedor { Codigo = codigo, RazonSocial = $"Proveedor {codigo}", Activo = true };
        contexto.Proveedor.Add(proveedor);
        await contexto.SaveChangesAsync();
        return proveedor.Id;
    }

    private static async Task<int> CrearArticuloAsync(PostgreSqlPruebas fixture, string nombre,
        params (int ProveedorId, string Codigo)[] codigos)
    {
        await using var contexto = fixture.CrearContexto();
        var alta = await new CrearArticuloUseCase(new ArticuloRepositorio(contexto), new ArticuloRepositorio(contexto),
                new ProveedorRepositorio(contexto), new UnidadDeTrabajo(contexto))
            .EjecutarAsync(new(nombre, "Estante 1",
                codigos.Select(c => new CodigoProveedorRequest(c.ProveedorId, c.Codigo)).ToArray()));
        Assert.True(alta.IsSuccess, alta.Error);
        return alta.Value.Id;
    }

    private static async Task<Articulo> LeerAsync(PostgreSqlPruebas fixture, int id)
    {
        await using var relectura = fixture.CrearContexto();
        return await relectura.Articulo.AsNoTracking().Include(a => a.ArticuloCodigoAlternativo)
            .SingleAsync(a => a.Id == id);
    }

    private static (string Codigo, int ProveedorId)[] Codigos(Articulo articulo) =>
        articulo.ArticuloCodigoAlternativo.OrderBy(c => c.ProveedorId).Select(c => (c.Codigo, c.ProveedorId)).ToArray();

    private static ModificarArticuloRequest Datos(string nombre, string? codigoInterno = null,
        params (int ProveedorId, string Codigo)[] codigos) =>
        new(nombre, " Pasillo 9 ", codigoInterno, codigos.Select(c => new CodigoProveedorRequest(c.ProveedorId, c.Codigo)).ToArray());

    // ---- CA1: por Id ----

    // La lección del Crítico 3: se actualiza por Id. Cambiar nombre, ubicación y código
    // interno de A no toca a B.
    [PostgreSqlFact]
    public async Task Modificar_ActualizaPorIdYNoTocaOtroArticulo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var a = await CrearArticuloAsync(fixture, "Bujía vieja");
        var b = await CrearArticuloAsync(fixture, "Filtro de aire");

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto, administrador: true)
                .EjecutarAsync(a, Datos(" Bujía NGK BPR6ES ", " bujia-01 "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        var modificado = await LeerAsync(fixture, a);
        Assert.Equal(("Bujía NGK BPR6ES", "Pasillo 9", "BUJIA-01", true),
            (modificado.Nombre, modificado.UbicacionDeposito, modificado.CodigoInterno, modificado.Activo));
        var otro = await LeerAsync(fixture, b);
        Assert.Equal(("Filtro de aire", "Estante 1", $"LC-{b:D6}"), (otro.Nombre, otro.UbicacionDeposito, otro.CodigoInterno));
    }

    // ---- CA2 ----

    [PostgreSqlFact]
    public async Task Modificar_AgregaElCodigoDeOtroProveedorYConservaLosExistentes()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        var id = await CrearArticuloAsync(fixture, "Bujía", (norte, "BPR-6ES"));
        var existente = (await LeerAsync(fixture, id)).ArticuloCodigoAlternativo.Single().Id;

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto).EjecutarAsync(id, Datos("Bujía", null, (norte, "bpr-6es"), (sur, " ngk 4321 ")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        var articulo = await LeerAsync(fixture, id);
        Assert.Equal(new[] { ("BPR-6ES", norte), ("NGK 4321", sur) }, Codigos(articulo));
        // El existente es la MISMA fila: no se borró y se volvió a crear.
        Assert.Equal(existente, articulo.ArticuloCodigoAlternativo.Single(c => c.ProveedorId == norte).Id);
    }

    // Omitir un código en una modificación ordinaria NO lo retira.
    [PostgreSqlFact]
    public async Task Modificar_SinCodigos_ConservaLosExistentes()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var id = await CrearArticuloAsync(fixture, "Bujía", (norte, "BPR-6ES"));

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto).EjecutarAsync(id, Datos("Bujía corregida"));

        Assert.True(resultado.IsSuccess, resultado.Error);
        var articulo = await LeerAsync(fixture, id);
        Assert.Equal("Bujía corregida", articulo.Nombre);
        Assert.Equal(new[] { ("BPR-6ES", norte) }, Codigos(articulo));
    }

    // ---- CA3 ----

    [PostgreSqlFact]
    public async Task Modificar_CodigoDeOtroArticulo_ConflictoInformandoCualYNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        var duenio = await CrearArticuloAsync(fixture, "Filtro de aire", (norte, "FA-100"));
        var id = await CrearArticuloAsync(fixture, "Bujía");

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto).EjecutarAsync(id, Datos("Bujía corregida", null, (sur, " fa-100 ")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains($"LC-{duenio:D6}", resultado.Error);
        Assert.Contains("Filtro de aire", resultado.Error);
        var articulo = await LeerAsync(fixture, id);
        Assert.Equal("Bujía", articulo.Nombre);
        Assert.Empty(articulo.ArticuloCodigoAlternativo);
    }

    // La unicidad entre código interno y códigos de proveedor solo la controla la aplicación
    // (no hay restricción entre tablas): se prueba contra la consulta real.
    [PostgreSqlFact]
    public async Task Modificar_CodigoInternoIgualAlCodigoDeProveedorDeOtroArticulo_Conflicto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        await CrearArticuloAsync(fixture, "Filtro de aire", (norte, "FA-100"));
        var id = await CrearArticuloAsync(fixture, "Bujía");

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto, administrador: true).EjecutarAsync(id, Datos("Bujía", " fa-100 "));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Equal($"LC-{id:D6}", (await LeerAsync(fixture, id)).CodigoInterno);
    }

    // ---- CA5 ----

    [PostgreSqlFact]
    public async Task Modificar_DadoDeBaja_InvalidoYNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var id = await CrearArticuloAsync(fixture, "Bujía");
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Baja(contexto, usuario).EjecutarAsync(id, new("Discontinuado"))).IsSuccess);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto).EjecutarAsync(id, Datos("Bujía corregida"));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("reactiv", resultado.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bujía", (await LeerAsync(fixture, id)).Nombre);
    }

    /// <summary>Lee con el repositorio real y, recién después, otra sesión da de baja el artículo.</summary>
    private sealed class ConsultaQueDaDeBajaDespuesDeLeer : IConsultaArticulosRepositorio
    {
        private readonly ArticuloRepositorio _real;
        private readonly PostgreSqlPruebas _fixture;

        public ConsultaQueDaDeBajaDespuesDeLeer(LaCentralDbContext contexto, PostgreSqlPruebas fixture)
            => (_real, _fixture) = (new ArticuloRepositorio(contexto), fixture);

        public async Task<ArticuloDetalleConsultaDto?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
        {
            var leido = await _real.ObtenerDetallePorIdAsync(id, ct);
            await using var otraSesion = _fixture.CrearContexto();
            await otraSesion.Articulo.Where(a => a.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Activo, false)
                .SetProperty(a => a.MotivoBaja, "Baja de otra sesión")
                .SetProperty(a => a.FechaBaja, DateTime.UtcNow), ct);
            return leido;
        }

        public Task<PaginaArticulosDto> BuscarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
            => _real.BuscarAsync(consulta, ct);

        public Task<IReadOnlyList<ArticuloResumenConsultaDto>> ObtenerConflictosCodigoAsync(string codigo, CancellationToken ct = default)
            => _real.ObtenerConflictosCodigoAsync(codigo, ct);
    }

    // La condición "activo" va en el UPDATE: vale lo que la fila es al escribir.
    [PostgreSqlFact]
    public async Task Modificar_OtraSesionLoDaDeBajaEntreLecturaYEscritura_InvalidoYNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        var id = await CrearArticuloAsync(fixture, "Bujía");

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto, consulta: new ConsultaQueDaDeBajaDespuesDeLeer(contexto, fixture))
                .EjecutarAsync(id, Datos("Bujía corregida", null, (sur, "NGK-1")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        var articulo = await LeerAsync(fixture, id);
        Assert.False(articulo.Activo);
        Assert.Equal(("Bujía", "Estante 1"), (articulo.Nombre, articulo.UbicacionDeposito));
        Assert.Empty(articulo.ArticuloCodigoAlternativo);
    }

    /// <summary>
    /// Valida con el repositorio real y, recién después, otra sesión le asigna el mismo
    /// código a otro artículo: la validación previa no cierra la carrera, el índice sí.
    /// </summary>
    private sealed class ConsultaConCarreraDeCodigo : IConsultaArticulosRepositorio
    {
        private readonly ArticuloRepositorio _real;
        private readonly PostgreSqlPruebas _fixture;
        private readonly int _otroArticulo;
        private readonly int _proveedor;

        public ConsultaConCarreraDeCodigo(LaCentralDbContext contexto, PostgreSqlPruebas fixture, int otroArticulo, int proveedor)
            => (_real, _fixture, _otroArticulo, _proveedor) = (new ArticuloRepositorio(contexto), fixture, otroArticulo, proveedor);

        public async Task<IReadOnlyList<ArticuloResumenConsultaDto>> ObtenerConflictosCodigoAsync(string codigo, CancellationToken ct = default)
        {
            var conflictos = await _real.ObtenerConflictosCodigoAsync(codigo, ct);
            await using var otraSesion = _fixture.CrearContexto();
            otraSesion.ArticuloCodigoAlternativo.Add(new ArticuloCodigoAlternativo
                { ArticuloId = _otroArticulo, ProveedorId = _proveedor, Codigo = NormalizacionArticulo.Codigo(codigo) });
            await otraSesion.SaveChangesAsync(ct);
            return conflictos;
        }

        public Task<ArticuloDetalleConsultaDto?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
            => _real.ObtenerDetallePorIdAsync(id, ct);

        public Task<PaginaArticulosDto> BuscarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
            => _real.BuscarAsync(consulta, ct);
    }

    // Atomicidad: el código choca en el MOTOR (codigo_unico_global) cuando los datos del
    // artículo ya se escribieron. No queda nada de la modificación.
    [PostgreSqlFact]
    public async Task Modificar_ElCodigoChocaEnElMotorDespuesDeActualizarLosDatos_NoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        var otro = await CrearArticuloAsync(fixture, "Filtro de aire");
        var id = await CrearArticuloAsync(fixture, "Bujía");

        await using (var contexto = fixture.CrearContexto())
        {
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() =>
                Modificar(contexto, consulta: new ConsultaConCarreraDeCodigo(contexto, fixture, otro, norte))
                    .EjecutarAsync(id, Datos("Bujía corregida", null, (sur, "NGK-1"))));
            Assert.Contains("codigo_unico_global", excepcion.InnerException?.Message);
        }

        var articulo = await LeerAsync(fixture, id);
        Assert.Equal(("Bujía", "Estante 1"), (articulo.Nombre, articulo.UbicacionDeposito));
        Assert.Empty(articulo.ArticuloCodigoAlternativo);
        Assert.Equal(new[] { ("NGK-1", norte) }, Codigos(await LeerAsync(fixture, otro)));
    }

    // ---- Retiro explícito ----

    // Borra el código de ESE proveedor en ESE artículo: el de otro proveedor y el del mismo
    // proveedor en otro artículo quedan.
    [PostgreSqlFact]
    public async Task Retirar_BorraSoloElCodigoDeEseProveedorEnEseArticulo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        var id = await CrearArticuloAsync(fixture, "Bujía", (norte, "BPR-6ES"), (sur, "NGK-1"));
        var otro = await CrearArticuloAsync(fixture, "Filtro de aire", (norte, "FA-100"));

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Retirar(contexto).EjecutarAsync(id, norte);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new[] { ("NGK-1", sur) }, Codigos(await LeerAsync(fixture, id)));
        Assert.Equal(new[] { ("FA-100", norte) }, Codigos(await LeerAsync(fixture, otro)));
    }

    // Retirado el código, el proveedor puede tener uno nuevo (D2) y el viejo queda libre.
    [PostgreSqlFact]
    public async Task Retirar_YDespuesAgregarOtroCodigoDelMismoProveedor_SeGuarda()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var id = await CrearArticuloAsync(fixture, "Bujía", (norte, "BPR-6ES"));

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Retirar(contexto).EjecutarAsync(id, norte)).IsSuccess);
        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Modificar(contexto).EjecutarAsync(id, Datos("Bujía", null, (norte, "BPR-7ES")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(new[] { ("BPR-7ES", norte) }, Codigos(await LeerAsync(fixture, id)));
    }

    // ---- Baja y reactivación (D10) ----

    [PostgreSqlFact]
    public async Task Baja_YReactivacion_EscribenLaFilaYElHistorialConLosUsuarios()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var quienBaja = await fixture.CrearUsuarioAsync("baja");
        var quienReactiva = await fixture.CrearUsuarioAsync("reactiva");
        var id = await CrearArticuloAsync(fixture, "Bujía");
        var otro = await CrearArticuloAsync(fixture, "Filtro de aire");

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Baja(contexto, quienBaja).EjecutarAsync(id, new(" Discontinuado "))).IsSuccess);

        var dadoDeBaja = await LeerAsync(fixture, id);
        Assert.False(dadoDeBaja.Activo);
        Assert.Equal("Discontinuado", dadoDeBaja.MotivoBaja);
        Assert.NotNull(dadoDeBaja.FechaBaja);
        Assert.True((await LeerAsync(fixture, otro)).Activo);
        await using (var relectura = fixture.CrearContexto())
        {
            var abierta = await relectura.ArticuloHistorialBaja.AsNoTracking().SingleAsync(h => h.ArticuloId == id);
            Assert.Equal(("Discontinuado", quienBaja, (DateTime?)null), (abierta.Motivo, abierta.UsuarioBajaId, abierta.FechaReactivacion));
            Assert.Equal(dadoDeBaja.FechaBaja!.Value, abierta.FechaBaja, TimeSpan.FromMilliseconds(1));
        }

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Reactivar(contexto, quienReactiva).EjecutarAsync(id)).IsSuccess);

        var reactivado = await LeerAsync(fixture, id);
        Assert.True(reactivado.Activo);
        // D10: la fila conserva el rastro de su última baja.
        Assert.Equal("Discontinuado", reactivado.MotivoBaja);
        Assert.Equal(dadoDeBaja.FechaBaja, reactivado.FechaBaja);
        await using (var relectura = fixture.CrearContexto())
        {
            var cerrada = await relectura.ArticuloHistorialBaja.AsNoTracking().SingleAsync(h => h.ArticuloId == id);
            Assert.NotNull(cerrada.FechaReactivacion);
            Assert.Equal(quienReactiva, cerrada.UsuarioReactivacionId);
        }
    }

    // Atomicidad: el historial falla en el MOTOR (ux_articulo_hist_abierta) después de
    // escribir la fila. El artículo sigue activo.
    [PostgreSqlFact]
    public async Task Baja_ElHistorialFallaEnElMotor_ElArticuloSigueActivo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var id = await CrearArticuloAsync(fixture, "Bujía");
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.ArticuloHistorialBaja.Add(new ArticuloHistorialBaja
                { ArticuloId = id, Motivo = "Baja abierta previa", FechaBaja = DateTime.UtcNow.AddDays(-1) });
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
        {
            var excepcion = await Assert.ThrowsAsync<DbUpdateException>(() =>
                Baja(contexto, usuario).EjecutarAsync(id, new("Discontinuado")));
            Assert.Contains("ux_articulo_hist_abierta", excepcion.InnerException?.Message);
        }

        var articulo = await LeerAsync(fixture, id);
        Assert.Equal((true, (string?)null, (DateTime?)null), (articulo.Activo, articulo.MotivoBaja, articulo.FechaBaja));
    }

    // Atomicidad: cerrar el historial falla en el MOTOR (articulo_hist_fechas_coherentes,
    // una baja con fecha futura) después de reactivar la fila. El artículo sigue inactivo.
    [PostgreSqlFact]
    public async Task Reactivar_ElHistorialFallaEnElMotor_ElArticuloSigueInactivo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var id = await CrearArticuloAsync(fixture, "Bujía");
        var futura = DateTime.UtcNow.AddDays(1);
        await using (var contexto = fixture.CrearContexto())
        {
            await contexto.Articulo.Where(a => a.Id == id).ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Activo, false).SetProperty(a => a.MotivoBaja, "Importado")
                .SetProperty(a => a.FechaBaja, futura));
            contexto.ArticuloHistorialBaja.Add(new ArticuloHistorialBaja { ArticuloId = id, Motivo = "Importado", FechaBaja = futura });
            await contexto.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
            await Assert.ThrowsAnyAsync<Exception>(() => Reactivar(contexto, usuario).EjecutarAsync(id));

        Assert.False((await LeerAsync(fixture, id)).Activo);
        await using var relectura = fixture.CrearContexto();
        Assert.Null((await relectura.ArticuloHistorialBaja.AsNoTracking().SingleAsync(h => h.ArticuloId == id)).FechaReactivacion);
    }

    // ---- Ficha ----

    // La ficha trae la ubicación (para editarla) y el motivo de la baja.
    [PostgreSqlFact]
    public async Task Detalle_TraeUbicacionYMotivoDeBaja()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var id = await CrearArticuloAsync(fixture, "Bujía");
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Baja(contexto, usuario).EjecutarAsync(id, new("Discontinuado"))).IsSuccess);

        await using var relectura = fixture.CrearContexto();
        var detalle = await new ArticuloRepositorio(relectura).ObtenerDetallePorIdAsync(id);

        Assert.NotNull(detalle);
        Assert.Equal(("Estante 1", "Discontinuado", false), (detalle.UbicacionDeposito, detalle.MotivoBaja, detalle.Activo));
    }
}

using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LaCentral.Tests.Proveedores;

/// <summary>Caso de uso + repositorio real; toda afirmación relee con un contexto nuevo.</summary>
[Trait("Proveedor", "PostgreSQL")]
public class ProveedorPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    // ALTO 5
    [PostgreSqlFact]
    public async Task DosAltasConElMismoCodigo_LaSegundaDaConflictoIdentificandoAlPrimero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-100", "Repuestos del Centro"))).IsSuccess);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-100", "Otro Proveedor"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Repuestos del Centro", resultado.Error);
        await using var lectura = fixture.CrearContexto();
        var filas = await lectura.Proveedor.AsNoTracking().Where(p => p.Codigo == "PRV-100").ToListAsync();
        Assert.Equal("Repuestos del Centro", Assert.Single(filas).RazonSocial);
    }

    // 1.4: la fecha de baja la transporta el dominio, no la inventa el repositorio
    [PostgreSqlFact]
    public async Task DarDeBaja_ReleidoConContextoNuevo_LaEntidadTraeMotivoYFecha()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-200", "Proveedor a dar de baja"))).IsSuccess);
        int id;
        await using (var contexto = fixture.CrearContexto())
            id = (await contexto.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "PRV-200")).Id;

        var antes = DateTime.UtcNow;
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await CasosDeBajaPostgreSql.DarDeBajaProveedor(contexto, await fixture.CrearUsuarioAsync())
                .EjecutarAsync(id, new DarDeBajaProveedorRequest("Cerró el local", "PRV-200"))).IsSuccess);

        await using var lectura = fixture.CrearContexto();
        var proveedor = (await new ProveedorRepositorio(lectura).ObtenerDetallePorIdAsync(id))!;
        var fila = await lectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == id);
        Assert.False(proveedor.Activo);
        Assert.Equal("Cerró el local", proveedor.MotivoBaja);
        Assert.NotNull(proveedor.FechaBaja);
        Assert.Equal(fila.FechaBaja, proveedor.FechaBaja);
        Assert.InRange(proveedor.FechaBaja!.Value, antes.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

// CRÍTICO 3 en proveedor: un Id inexistente no sale en silencio ni pisa otra fila.
    [PostgreSqlFact]
    public async Task ActualizarConIdInexistente_DevuelveFalseYNoTocaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-300", "Proveedor Real"))).IsSuccess);
        int id;
        await using (var contexto = fixture.CrearContexto())
            id = (await contexto.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "PRV-300")).Id;

        bool actualizado;
        await using (var contexto = fixture.CrearContexto())
            actualizado = await new ProveedorRepositorio(contexto).ActualizarAsync(new LaCentral.UseCases.Entidades.Proveedor
                { Id = id + 1000, Codigo = "PRV-300", RazonSocial = "Fantasma", Activo = true });

        Assert.False(actualizado);
        await using var lectura = fixture.CrearContexto();
        Assert.Equal("Proveedor Real", Assert.Single(await lectura.Proveedor.AsNoTracking().ToListAsync()).RazonSocial);
    }

// Pérdida de datos silenciosa presente en develop: modificar devolviendo lo que se leyó
    // borraba la dirección sin calle y, con ella, su localidad.
    [PostgreSqlFact]
    public async Task ModificarConDireccionSinCalle_LaConservaConSuLocalidad()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedor = new LaCentral.Data.Models.Proveedor { Codigo = "PRV-DIR", RazonSocial = "Proveedor Sin Calle", Cuit = "30-11111111-1", Activo = true };
        proveedor.ProveedorDireccion.Add(new LaCentral.Data.Models.ProveedorDireccion { Calle = null, Localidad = "Córdoba" });
        proveedor.ProveedorDireccion.Add(new LaCentral.Data.Models.ProveedorDireccion { Calle = "Ruta 9 km 5" });
        await using (var setup = fixture.CrearContexto())
        {
            setup.Proveedor.Add(proveedor);
            await setup.SaveChangesAsync();
        }
        var sinCalleId = proveedor.ProveedorDireccion.Single(d => d.Calle == null).Id;

        await using (var contexto = fixture.CrearContexto())
        {
            var repo = new ProveedorRepositorio(contexto);
            var leido = (await repo.ObtenerDetallePorIdAsync(proveedor.Id))!;
            var resultado = await new ModificarProveedorUseCase(repo).EjecutarAsync(proveedor.Id,
                new ModificarProveedorRequest("Proveedor Renombrado", leido.Cuit!, null, null, leido.Direcciones));
            Assert.True(resultado.IsSuccess);
        }

        await using var lectura = fixture.CrearContexto();
        var direcciones = await lectura.ProveedorDireccion.AsNoTracking().Where(d => d.ProveedorId == proveedor.Id).ToListAsync();
        Assert.Equal(2, direcciones.Count);
        var sinCalle = Assert.Single(direcciones, d => d.Calle == null);
        Assert.Equal(sinCalleId, sinCalle.Id);
        Assert.Equal("Córdoba", sinCalle.Localidad);
    }

    // Conservar no es crear: una dirección vacía que no existía no genera una fila vacía.
    [PostgreSqlFact]
    public async Task ModificarConDireccionVaciaNueva_NoCreaUnaFilaVacia()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-400", "Proveedor Sin Direcciones") with { Cuit = "30-22222222-2" })).IsSuccess);
        int id;
        await using (var contexto = fixture.CrearContexto())
            id = (await contexto.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "PRV-400")).Id;

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new ModificarProveedorUseCase(new ProveedorRepositorio(contexto)).EjecutarAsync(id,
                new ModificarProveedorRequest("Proveedor Sin Direcciones", "30-22222222-2", null, null, new List<string> { "  " }))).IsSuccess);

        await using var lectura = fixture.CrearContexto();
        Assert.False(await lectura.ProveedorDireccion.AnyAsync(d => d.ProveedorId == id));
    }

    private static CrearProveedorRequest Alta(string codigo, string razonSocial) =>
        new(codigo, razonSocial, null, null, null, null);
}

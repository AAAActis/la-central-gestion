using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// H1 del auditor: cada restricción de articulo_precio_modalidad, contra el motor. Cada
/// fila viola UNA sola restricción (las demás se cumplen), y se relee con un contexto nuevo
/// que no quedó nada. articulo_precio_modalidad_valida está en ArticuloPrecioModalidadPostgreSqlTests.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class ArticuloPrecioModalidadRestriccionesPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ArticuloPrecioModalidadRestriccionesPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static readonly DateTime Fecha = new(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);

    private sealed record Sembrado(PostgreSqlPruebas Fixture, int ArticuloId, int UsuarioId);

    private async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuarioId = await fixture.CrearUsuarioAsync();
        await using var contexto = fixture.CrearContexto();
        var articulo = new Articulo { CodigoInterno = "ART-H1", Nombre = "Artículo H1", Activo = true };
        contexto.Articulo.Add(articulo);
        await contexto.SaveChangesAsync();
        return new(fixture, articulo.Id, usuarioId);
    }

    private static async Task AfirmarRechazoAsync(
        Sembrado s, ArticuloPrecioModalidad fila, string sqlState, string restriccion)
    {
        await using (var contexto = s.Fixture.CrearContexto())
        {
            contexto.ArticuloPrecioModalidad.Add(fila);
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(sqlState, postgres.SqlState);
            Assert.Equal(restriccion, postgres.ConstraintName);
        }

        await using var relectura = s.Fixture.CrearContexto();
        Assert.False(await relectura.ArticuloPrecioModalidad.AnyAsync(
            p => p.ArticuloId == fila.ArticuloId && p.Modalidad == fila.Modalidad && p.Precio == fila.Precio
                && p.Porcentaje == fila.Porcentaje && p.CalculoManual == fila.CalculoManual));
    }

    private static Task AfirmarCheckAsync(Sembrado s, ArticuloPrecioModalidad fila, string restriccion)
        => AfirmarRechazoAsync(s, fila, PostgresErrorCodes.CheckViolation, restriccion);

    [PostgreSqlFact]
    public async Task Check_ManualSinPorcentaje()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "CostoArticulo", Porcentaje = 5m, CalculoManual = true
        }, "articulo_precio_modalidad_manual_sin_porcentaje");
    }

    [PostgreSqlFact]
    public async Task Check_CalculadoConPorcentaje()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Precio = 150m, FechaCalculo = Fecha
        }, "articulo_precio_modalidad_calculado_con_porcentaje");
    }

    [PostgreSqlFact]
    public async Task Check_PrecioConFecha()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "CostoArticulo", Porcentaje = 50m, Precio = 150m
        }, "articulo_precio_modalidad_precio_con_fecha");
    }

    [PostgreSqlFact]
    public async Task Check_ManualConUsuario()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "CasoEspecial", CalculoManual = true, Precio = 120m, FechaCalculo = Fecha
        }, "articulo_precio_modalidad_manual_con_usuario");
    }

    [PostgreSqlFact]
    public async Task Check_PorcentajeRango_RechazaMenosDeMenos100YAceptaMenos100()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "EfectivoTransferencia", Base = "PrecioLista", Porcentaje = -100.01m
        }, "articulo_precio_modalidad_porcentaje_rango");

        // El límite: -100 entra. Y un negativo es válido (Efectivo es un descuento).
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.ArticuloPrecioModalidad.Add(new()
            {
                ArticuloId = s.ArticuloId, Modalidad = "EfectivoTransferencia", Base = "PrecioLista", Porcentaje = -100m
            });
            await contexto.SaveChangesAsync();
        }
        await using var relectura = fixture.CrearContexto();
        Assert.Equal(-100m, (await relectura.ArticuloPrecioModalidad.SingleAsync(p => p.ArticuloId == s.ArticuloId)).Porcentaje);
    }

    [PostgreSqlFact]
    public async Task Check_PrecioNoNegativo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "CostoArticulo", Porcentaje = 50m, Precio = -1m, FechaCalculo = Fecha
        }, "articulo_precio_modalidad_precio_no_negativo");
    }

    [PostgreSqlFact]
    public async Task Check_BaseValida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        // "Costo" es la modalidad, no una base.
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Costo", Base = "Costo", Porcentaje = 0m
        }, "articulo_precio_modalidad_base_valida");
    }

    [PostgreSqlFact]
    public async Task Check_BaseCoherente_EnLosDosSentidos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "CostoArticulo"
        }, "articulo_precio_modalidad_base_coherente");
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Tarjeta3", Porcentaje = 5m
        }, "articulo_precio_modalidad_base_coherente");
    }

    [PostgreSqlFact]
    public async Task Check_BaseSinAutorreferencia()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarCheckAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "PrecioLista", Porcentaje = 50m
        }, "articulo_precio_modalidad_base_sin_autorreferencia");
    }

    [PostgreSqlFact]
    public async Task ClavePrimaria_UnaFilaPorArticuloYModalidad()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.ArticuloPrecioModalidad.Add(new() { ArticuloId = s.ArticuloId, Modalidad = "Tarjeta3" });
            await contexto.SaveChangesAsync();
        }

        await AfirmarRechazoAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "Tarjeta3", Base = "CostoArticulo", Porcentaje = 7m
        }, PostgresErrorCodes.UniqueViolation, "articulo_precio_modalidad_pkey");
    }

    [PostgreSqlFact]
    public async Task ClaveForanea_ArticuloInexistente()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        // Un Id que la base no generó: no se siembra una fila, se referencia una ausente.
        await AfirmarRechazoAsync(s, new()
        {
            ArticuloId = s.ArticuloId + 1000, Modalidad = "Tarjeta3"
        }, PostgresErrorCodes.ForeignKeyViolation, "articulo_precio_modalidad_articulo_fkey");
    }

    [PostgreSqlFact]
    public async Task ClaveForanea_UsuarioInexistente()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await AfirmarRechazoAsync(s, new()
        {
            ArticuloId = s.ArticuloId, Modalidad = "CasoEspecial", CalculoManual = true, Precio = 120m,
            FechaCalculo = Fecha, UsuarioId = s.UsuarioId + 1000
        }, PostgresErrorCodes.ForeignKeyViolation, "articulo_precio_modalidad_usuario_fkey");
    }

    [PostgreSqlFact]
    public async Task BorrarArticulo_BorraSusPreciosEnCascada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.ArticuloPrecioModalidad.AddRange(
                new() { ArticuloId = s.ArticuloId, Modalidad = "Lista", Base = "CostoArticulo", Porcentaje = 50m },
                new() { ArticuloId = s.ArticuloId, Modalidad = "Tarjeta3" });
            await contexto.SaveChangesAsync();
        }

        await using (var borrado = fixture.CrearContexto())
            Assert.Equal(1, await borrado.Articulo.Where(a => a.Id == s.ArticuloId).ExecuteDeleteAsync());

        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.ArticuloPrecioModalidad.AnyAsync(p => p.ArticuloId == s.ArticuloId));
    }
}

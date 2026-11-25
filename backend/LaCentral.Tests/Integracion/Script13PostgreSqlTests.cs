using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// El script 13 (D-S7-03 costo previo por línea, D-S7-04 anulación de un solo administrador)
/// contra PostgreSQL real. Cada prueba corre el archivo versionado tal cual sobre su esquema
/// aislado: el 13 es idempotente, así que vale igual antes y después de que esté en public.
/// Lo que tiene que fallar se verifica por código SQL y nombre de restricción; lo que tiene
/// que entrar se relee con una conexión nueva.
/// </summary>
[Trait("Esquema", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class Script13PostgreSqlTests
{
    private const string ViolacionCheck = "23514";
    private readonly ITestOutputHelper _salida;
    public Script13PostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static string LeerScript()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ruta = Path.Combine(dir.FullName, "backend", "db", "rescaffold", "13_Anulacion_Costo_Previo_Un_Administrador.sql");
            if (File.Exists(ruta)) return File.ReadAllText(ruta);
        }
        throw new FileNotFoundException("No encontré backend/db/rescaffold/13_Anulacion_Costo_Previo_Un_Administrador.sql.");
    }

    private static async Task<List<string>> AplicarAsync(PostgreSqlPruebas fixture)
    {
        var avisos = new List<string>();
        await using var conexion = new NpgsqlConnection(fixture.ConexionAislada);
        conexion.Notice += (_, e) => avisos.Add(e.Notice.MessageText);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(LeerScript(), conexion) { CommandTimeout = 120 };
        await comando.ExecuteNonQueryAsync();
        return avisos;
    }

    private static async Task<T?> EscalarAsync<T>(PostgreSqlPruebas fixture, string sql)
    {
        await using var conexion = new NpgsqlConnection(fixture.ConexionAislada);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        var valor = await comando.ExecuteScalarAsync();
        return valor is DBNull or null ? default : (T)valor;
    }

    private static async Task EjecutarAsync(PostgreSqlPruebas fixture, string sql)
    {
        await using var conexion = new NpgsqlConnection(fixture.ConexionAislada);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task FallaConAsync(Func<Task> accion, string restriccion)
    {
        var pg = await Assert.ThrowsAsync<PostgresException>(accion);
        Assert.Equal((ViolacionCheck, restriccion), (pg.SqlState, pg.ConstraintName));
    }

    private static async Task FallaConAsync(LaCentralDbContext contexto, string restriccion)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal((ViolacionCheck, restriccion), (pg.SqlState, pg.ConstraintName));
    }

    /// <summary>Usuario, proveedor, artículo y factura ACTIVA. Los Ids los genera la base.</summary>
    private static async Task<(int Usuario, int Proveedor, int Articulo, int Factura, short Sucursal)> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuario = await fixture.CrearUsuarioAsync();
        await using var contexto = fixture.CrearContexto();
        var proveedor = new Proveedor { Codigo = "PRV-13", RazonSocial = "Proveedor 13", Activo = true };
        var articulo = new Articulo { CodigoInterno = "ART-13", Nombre = "Artículo 13", Activo = true };
        contexto.AddRange(proveedor, articulo);
        await contexto.SaveChangesAsync();
        var factura = new FacturaCompra
        {
            ProveedorId = proveedor.Id, Numero = "A-13", FechaEmision = new DateOnly(2026, 11, 25),
            Estado = "ACTIVA", Origen = "MANUAL", UsuarioId = usuario, FechaRegistro = DateTime.UtcNow
        };
        contexto.FacturaCompra.Add(factura);
        await contexto.SaveChangesAsync();
        var sucursal = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
        return (usuario, proveedor.Id, articulo.Id, factura.Id, sucursal);
    }

    private static string Linea((int Usuario, int Proveedor, int Articulo, int Factura, short Sucursal) s, string columnas, string valores) =>
        $"INSERT INTO factura_compra_detalle (factura_compra_id, articulo_id, sucursal_id, cantidad, precio_unitario{columnas}) " +
        $"VALUES ({s.Factura}, {s.Articulo}, {s.Sucursal}, 1, 10{valores})";

    [PostgreSqlFact]
    public async Task Script_SeAplicaDosVeces_YLaSegundaNoCambiaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);

        var primera = await AplicarAsync(fixture);
        var segunda = await AplicarAsync(fixture);

        Assert.Contains("13: TODO OK.", primera);
        Assert.Equal(["there is no transaction in progress", "13: TODO OK."], segunda);
    }

    // Una rama sin el cambio del caso de uso inserta sin las columnas: la línea no queda marcada como guardada.
    [PostgreSqlFact]
    public async Task LineaSinCostoPrevio_QuedaNoGuardadaYSinCosto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await EjecutarAsync(fixture, Linea(s, "", ""));

        Assert.Equal("false|NULL", await EscalarAsync<string>(fixture,
            $"SELECT costo_previo_guardado::text || '|' || coalesce(costo_previo::text, 'NULL') FROM factura_compra_detalle WHERE factura_compra_id = {s.Factura}"));
    }

    [PostgreSqlFact]
    public async Task CostoPrevioGuardado_EntraConValorYConDesconocido()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.Articulo.Add(new Articulo { CodigoInterno = "ART-13B", Nombre = "Otro", Activo = true });
            await contexto.SaveChangesAsync();
        }
        var otro = await EscalarAsync<int>(fixture, "SELECT id FROM articulo WHERE codigo_interno = 'ART-13B'");

        await EjecutarAsync(fixture, Linea(s, ", costo_previo, costo_previo_guardado", ", 1234.50, true"));
        await EjecutarAsync(fixture, Linea(s with { Articulo = otro }, ", costo_previo, costo_previo_guardado", ", NULL, true"));

        Assert.Equal("1234.50 true; NULL true", await EscalarAsync<string>(fixture,
            $"SELECT string_agg(coalesce(costo_previo::text, 'NULL') || ' ' || costo_previo_guardado::text, '; ' ORDER BY costo_previo NULLS LAST) " +
            $"FROM factura_compra_detalle WHERE factura_compra_id = {s.Factura}"));
    }

    [PostgreSqlFact]
    public async Task CostoPrevioNegativo_LoRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await FallaConAsync(() => EjecutarAsync(fixture, Linea(s, ", costo_previo, costo_previo_guardado", ", -0.01, true")),
            "fcd_costo_previo_no_negativo");
    }

    [PostgreSqlFact]
    public async Task CostoPrevioSinMarcarGuardado_LoRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await FallaConAsync(() => EjecutarAsync(fixture, Linea(s, ", costo_previo", ", 100")), "fcd_costo_previo_coherente");
    }

    [PostgreSqlFact]
    public async Task FacturaAnuladaSinQuienAnulo_LaRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await using var contexto = fixture.CrearContexto();
        var factura = await contexto.FacturaCompra.SingleAsync(f => f.Id == s.Factura);
        (factura.Estado, factura.MotivoAnulacion, factura.FechaAnulacion) = ("ANULADA", "Mal cargada", DateTime.UtcNow);
        await FallaConAsync(contexto, "fc_anulacion_con_usuario");
    }

    // D-S7-04: anula un ADMINISTRADOR solo; nadie más autoriza.
    [PostgreSqlFact]
    public async Task FacturaAnuladaPorUnoSinAutorizacion_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await using (var contexto = fixture.CrearContexto())
        {
            var factura = await contexto.FacturaCompra.SingleAsync(f => f.Id == s.Factura);
            (factura.Estado, factura.MotivoAnulacion, factura.FechaAnulacion, factura.UsuarioAnulacionId) =
                ("ANULADA", "Mal cargada", DateTime.UtcNow, s.Usuario);
            await contexto.SaveChangesAsync();
        }

        await using var nuevo = fixture.CrearContexto();
        var leida = await nuevo.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == s.Factura);
        Assert.Equal(("ANULADA", (int?)s.Usuario, (int?)null), (leida.Estado, leida.UsuarioAnulacionId, leida.UsuarioAutorizacionId));
    }

    private static NotaCreditoCompra Nota(int proveedor, int usuario) => new()
    {
        ProveedorId = proveedor, Numero = "NC-13", FechaEmision = new DateOnly(2026, 11, 25), Clase = "DEVOLUCION",
        ImporteNeto = 100m, Origen = "MANUAL", UsuarioId = usuario, FechaRegistro = DateTime.UtcNow, Estado = "ACTIVA"
    };

    [PostgreSqlFact]
    public async Task NotaAnuladaPorUnoSinAutorizacion_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);
        var nota = Nota(s.Proveedor, s.Usuario);
        (nota.Estado, nota.MotivoAnulacion, nota.FechaAnulacion, nota.UsuarioAnulacionId) = ("ANULADA", "Duplicada", DateTime.UtcNow, s.Usuario);
        await using (var contexto = fixture.CrearContexto())
        {
            contexto.NotaCreditoCompra.Add(nota);
            await contexto.SaveChangesAsync();
        }

        await using var nuevo = fixture.CrearContexto();
        var leida = await nuevo.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == nota.Id);
        Assert.Equal(("ANULADA", (int?)s.Usuario, (int?)null), (leida.Estado, leida.UsuarioAnulacionId, leida.UsuarioAutorizacionId));
    }

    [PostgreSqlFact]
    public async Task NotaAnuladaSinQuienAnulo_LaRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);
        var nota = Nota(s.Proveedor, s.Usuario);
        (nota.Estado, nota.MotivoAnulacion, nota.FechaAnulacion) = ("ANULADA", "Duplicada", DateTime.UtcNow);

        await using var contexto = fixture.CrearContexto();
        contexto.NotaCreditoCompra.Add(nota);
        await FallaConAsync(contexto, "ncc_anulacion_coherente");
    }

    [PostgreSqlFact]
    public async Task NotaActivaConAutorizacion_LaRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);
        var nota = Nota(s.Proveedor, s.Usuario);
        nota.UsuarioAutorizacionId = s.Usuario;

        await using var contexto = fixture.CrearContexto();
        contexto.NotaCreditoCompra.Add(nota);
        await FallaConAsync(contexto, "ncc_anulacion_coherente");
    }

    // En la real puede haber anuladas sin usuario: el 13 corta con un mensaje y no deja nada a medias.
    // Sin aplicar antes: mientras public no tenga el 13, también prueba que los pasos 1 y 2 se deshacen.
    [PostgreSqlFact]
    public async Task ConFacturaAnuladaSinUsuario_ElScriptCortaSinAplicarNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await EjecutarAsync(fixture, "ALTER TABLE factura_compra DROP CONSTRAINT IF EXISTS fc_anulacion_con_usuario; " +
            $"UPDATE factura_compra SET estado = 'ANULADA', motivo_anulacion = 'Vieja', fecha_anulacion = now() WHERE id = {s.Factura}");
        const string firma = "SELECT string_agg(conname || ':' || pg_get_constraintdef(oid), ';' ORDER BY conname) FROM pg_constraint " +
            "WHERE connamespace = current_schema()::regnamespace";
        var antes = await EscalarAsync<string>(fixture, firma);

        var error = await Assert.ThrowsAsync<PostgresException>(() => AplicarAsync(fixture));

        Assert.Equal("P0001", error.SqlState);
        Assert.Contains("1 facturas de compra anuladas sin usuario_anulacion_id", error.MessageText);
        Assert.Equal(antes, await EscalarAsync<string>(fixture, firma));
    }
}

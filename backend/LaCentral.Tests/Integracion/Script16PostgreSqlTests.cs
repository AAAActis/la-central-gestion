using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// El script 16 (D2 revisada, D-S7-06; vinculación con quién y cuándo, D-S7-07) contra
/// PostgreSQL real. Cada prueba corre el archivo versionado tal cual sobre su esquema aislado:
/// es idempotente, así que vale igual antes y después de que esté en public. Lo que falla se
/// verifica por código SQL y nombre; lo que entra se relee con una conexión nueva.
/// </summary>
[Trait("Esquema", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class Script16PostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public Script16PostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static string LeerScript()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ruta = Path.Combine(dir.FullName, "backend", "db", "rescaffold", "16_Vinculacion_Codigo_Proveedor.sql");
            if (File.Exists(ruta)) return File.ReadAllText(ruta);
        }
        throw new FileNotFoundException("No encontré backend/db/rescaffold/16_Vinculacion_Codigo_Proveedor.sql.");
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

    private static async Task<string?> LeerAsync(PostgreSqlPruebas fixture, string sql)
    {
        await using var conexion = new NpgsqlConnection(fixture.ConexionAislada);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        return (await comando.ExecuteScalarAsync()) as string;
    }

    private static async Task EjecutarAsync(PostgreSqlPruebas fixture, string sql)
    {
        await using var conexion = new NpgsqlConnection(fixture.ConexionAislada);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexion);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task FallaConAsync(Func<Task> accion, string sqlState, string restriccion)
    {
        var pg = await Assert.ThrowsAsync<PostgresException>(accion);
        Assert.Equal((sqlState, restriccion), (pg.SqlState, pg.ConstraintName));
    }

    /// <summary>Usuario, dos proveedores y dos artículos. Los Ids los genera la base.</summary>
    private static async Task<(int Usuario, int Norte, int Sur, int Bomba, int Filtro)> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuario = await fixture.CrearUsuarioAsync();
        await using var contexto = fixture.CrearContexto();
        var norte = new Proveedor { Codigo = "PRV-N", RazonSocial = "Norte", Activo = true };
        var sur = new Proveedor { Codigo = "PRV-S", RazonSocial = "Sur", Activo = true };
        var bomba = new Articulo { CodigoInterno = "ART-B", Nombre = "Bomba de agua", Activo = true };
        var filtro = new Articulo { CodigoInterno = "ART-F", Nombre = "Filtro de aceite", Activo = true };
        contexto.AddRange(norte, sur, bomba, filtro);
        await contexto.SaveChangesAsync();
        return (usuario, norte.Id, sur.Id, bomba.Id, filtro.Id);
    }

    // Sin auditoría, el INSERT de siempre (el de la ficha del artículo): no nombra las columnas nuevas.
    private static string Codigo(int articulo, int proveedor, string codigo, string? auditoria = null) => auditoria is null
        ? $"INSERT INTO articulo_codigo_alternativo (articulo_id, proveedor_id, codigo) VALUES ({articulo}, {proveedor}, '{codigo}')"
        : "INSERT INTO articulo_codigo_alternativo (articulo_id, proveedor_id, codigo, usuario_id, fecha_vinculacion) " +
          $"VALUES ({articulo}, {proveedor}, '{codigo}', {auditoria})";

    private const string Codigos =
        "SELECT string_agg(articulo_id || ':' || proveedor_id || ':' || codigo, ', ' ORDER BY articulo_id, proveedor_id, codigo) " +
        "FROM articulo_codigo_alternativo";

    [PostgreSqlFact]
    public async Task Script_SeAplicaDosVeces_YLaSegundaNoCambiaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);

        var primera = await AplicarAsync(fixture);
        var segunda = await AplicarAsync(fixture);

        Assert.Contains("16: TODO OK.", primera);
        Assert.Equal(["there is no transaction in progress", "16: TODO OK."], segunda);
    }

    // D2 revisada: la unicidad es por proveedor, no global.
    [PostgreSqlFact]
    public async Task MismoCodigoEnDosProveedores_ParaArticulosDistintos_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "1234"));
        await EjecutarAsync(fixture, Codigo(s.Filtro, s.Sur, "1234"));

        Assert.Equal($"{s.Bomba}:{s.Norte}:1234, {s.Filtro}:{s.Sur}:1234", await LeerAsync(fixture, Codigos));
    }

    // 054 CA3: la misma bomba en dos marcas, cada una con su código del mismo proveedor.
    [PostgreSqlFact]
    public async Task DosCodigosDelMismoProveedor_ParaElMismoArticulo_Entran()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "BOMBA-SKF"));
        await EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "BOMBA-DOLZ"));

        Assert.Equal($"{s.Bomba}:{s.Norte}:BOMBA-DOLZ, {s.Bomba}:{s.Norte}:BOMBA-SKF", await LeerAsync(fixture, Codigos));
    }

    // Cada código de un proveedor apunta a un solo artículo: la factura de compra no queda ambigua.
    [PostgreSqlFact]
    public async Task MismoCodigoDelMismoProveedor_EnDosArticulos_LoRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);
        await EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "1234"));

        await FallaConAsync(() => EjecutarAsync(fixture, Codigo(s.Filtro, s.Norte, "1234")), "23505", "codigo_alt_proveedor_codigo_unico");
    }

    [PostgreSqlFact]
    public async Task VinculacionConQuienYCuando_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "1234", $"{s.Usuario}, TIMESTAMPTZ '2026-11-25 10:00:00+00'"));

        Assert.Equal($"{s.Usuario} 2026-11-25 10:00:00+00", await LeerAsync(fixture,
            "SELECT usuario_id || ' ' || to_char(fecha_vinculacion AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI:SS') || '+00' FROM articulo_codigo_alternativo"));
    }

    [PostgreSqlFact]
    public async Task QuienSinCuando_LoRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await FallaConAsync(() => EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "1234", $"{s.Usuario}, NULL")),
            "23514", "aca_vinculacion_coherente");
    }

    [PostgreSqlFact]
    public async Task CuandoSinQuien_LoRechazaLaBase()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);
        var s = await SembrarAsync(fixture);

        await FallaConAsync(() => EjecutarAsync(fixture, Codigo(s.Bomba, s.Norte, "1234", "NULL, now()")),
            "23514", "aca_vinculacion_coherente");
    }

    // Los índices viejos no quedan: si quedara un_codigo_por_proveedor, el CA3 seguiría rechazado.
    [PostgreSqlFact]
    public async Task Script_SacaLosIndicesViejos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await AplicarAsync(fixture);

        Assert.Equal("articulo_codigo_alternativo_pkey, codigo_alt_proveedor_codigo_unico, ix_codigo_alt_articulo", await LeerAsync(fixture,
            "SELECT string_agg(relname, ', ' ORDER BY relname) FROM pg_index i JOIN pg_class x ON x.oid = i.indexrelid " +
            "WHERE i.indrelid = 'articulo_codigo_alternativo'::regclass"));
    }
}

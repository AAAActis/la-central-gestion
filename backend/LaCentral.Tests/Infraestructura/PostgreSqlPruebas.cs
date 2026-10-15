using System.Text.RegularExpressions;
using LaCentral.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// Marca un test que corre contra PostgreSQL real. NUNCA se omite: sin conexión
/// configurada falla (ver PostgreSqlPruebas.ConexionRequerida). Una prueba no
/// ejecutada no es una prueba aprobada.
/// </summary>
public sealed class PostgreSqlFactAttribute : FactAttribute
{
}

/// <summary>
/// Copia SOLO estructura desde la base de test explícita a un esquema propio.
/// No crea un esquema simplificado ni toca tablas, secuencias o datos del origen.
/// </summary>
public sealed class PostgreSqlPruebas : IAsyncDisposable
{
    public static string? ConexionConfigurada
    {
        get
        {
            var principal = Environment.GetEnvironmentVariable("LA_CENTRAL_TEST_POSTGRES");
            var alias = Environment.GetEnvironmentVariable("LACENTRAL_TEST_PG");
            return !string.IsNullOrWhiteSpace(principal) ? principal :
                !string.IsNullOrWhiteSpace(alias) ? alias : null;
        }
    }

    /// <summary>La conexión de test, o una falla que explica cómo configurarla.</summary>
    public static string ConexionRequerida => ConexionConfigurada ?? throw new InvalidOperationException(
        "Falta la conexión PostgreSQL de test: definí LACENTRAL_TEST_PG (o LA_CENTRAL_TEST_POSTGRES) " +
        "apuntando a una base test_* o *_test. Ver LaCentral.Tests/README.md.");

    private readonly NpgsqlConnection _administracion;
    private readonly string _esquema;
    private readonly string _conexionAislada;
    private bool _creado;

    private PostgreSqlPruebas(NpgsqlConnection administracion, string esquema, string conexionAislada)
        => (_administracion, _esquema, _conexionAislada) = (administracion, esquema, conexionAislada);

    /// <summary>Cadena con search_path al esquema aislado: para levantar la API contra él.</summary>
    public string ConexionAislada => _conexionAislada;

    public LaCentralDbContext CrearContexto() => new(new DbContextOptionsBuilder<LaCentralDbContext>()
        .UseNpgsql(_conexionAislada).EnableDetailedErrors().Options);

    /// <param name="trasCrearEsquema">
    /// Solo para probar la limpieza: corre sobre la conexión de administración apenas
    /// creado el esquema, para forzar una falla real a mitad de la creación.
    /// </param>
    public static async Task<PostgreSqlPruebas> CrearAsync(ITestOutputHelper salida,
        Func<NpgsqlConnection, string, Task>? trasCrearEsquema = null)
    {
        var conexion = ConexionRequerida;
        var alias = Environment.GetEnvironmentVariable("LACENTRAL_TEST_PG");
        var principal = Environment.GetEnvironmentVariable("LA_CENTRAL_TEST_POSTGRES");
        if (!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(principal) && alias != principal)
            throw new InvalidOperationException("Las dos variables de conexión de test tienen valores distintos.");

        var config = new NpgsqlConnectionStringBuilder(conexion);
        ValidarBase(config.Database);
        var origen = Environment.GetEnvironmentVariable("LA_CENTRAL_TEST_SCHEMA") ?? "public";
        if (!Regex.IsMatch(origen, "^[a-z_][a-z0-9_]*$"))
            throw new InvalidOperationException("El esquema fuente de test no es un identificador válido.");
        var esquema = "pr110_test_" + Guid.NewGuid().ToString("N");
        // La conexión de catálogo no ve public por search_path: las definiciones
        // de FK retornan nombres calificados y se pueden redirigir sin ambigüedad.
        config.SearchPath = "pg_catalog";
        config.Timeout = 10;
        config.CommandTimeout = 30;
        var admin = new NpgsqlConnection(config.ConnectionString);
        config.SearchPath = esquema;
        // Identifica en pg_stat_activity las conexiones del pool de este esquema.
        config.ApplicationName = esquema;
        var fixture = new PostgreSqlPruebas(admin, esquema, config.ConnectionString);
        try
        {
            await admin.OpenAsync();
            await using (var comando = new NpgsqlCommand("SELECT current_database()", admin))
            {
                var actual = (string)(await comando.ExecuteScalarAsync())!;
                ValidarBase(actual);
                if (actual != config.Database) throw new InvalidOperationException("La base efectiva no coincide con la configurada.");
            }
            await using var modelo = fixture.CrearContexto();
            var entidades = modelo.Model.GetEntityTypes().Where(e => e.GetTableName() is not null).ToArray();
            var tablas = entidades.Select(e => e.GetTableName()!).Distinct().Order().ToArray();

            // Fallar ANTES de clonar/cargar si la plantilla real no coincide.
            foreach (var entidad in entidades)
                await fixture.ValidarTablaAsync(origen, entidad);

            await fixture.EjecutarAsync($"CREATE SCHEMA {Q(esquema)}");
            fixture._creado = true;
            if (trasCrearEsquema is not null) await trasCrearEsquema(admin, esquema);
            foreach (var tabla in tablas)
                await fixture.EjecutarAsync($"CREATE TABLE {Q(esquema)}.{Q(tabla)} (LIKE {Q(origen)}.{Q(tabla)} INCLUDING ALL)");
            await fixture.RestaurarNombresDeIndicesAsync(origen, tablas);

            // LIKE conserva tipos, defaults, CHECK, índices e identity (con
            // secuencias nuevas), pero NO copia las FK: se agregan explícitamente.
            foreach (var tabla in tablas)
            {
                var restricciones = new List<(string Nombre, string Definicion, string EsquemaDestino, string TablaDestino)>();
                await using var comando = new NpgsqlCommand("""
                    SELECT c.conname, pg_get_constraintdef(c.oid), nr.nspname, r.relname
                    FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    JOIN pg_class r ON r.oid = c.confrelid
                    JOIN pg_namespace nr ON nr.oid = r.relnamespace
                    WHERE c.contype = 'f' AND n.nspname = @schema AND t.relname = @table
                    """, admin);
                comando.Parameters.AddWithValue("schema", origen);
                comando.Parameters.AddWithValue("table", tabla);
                await using (var reader = await comando.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                        restricciones.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3)));
                foreach (var fk in restricciones)
                {
                    if (fk.EsquemaDestino != origen || !tablas.Contains(fk.TablaDestino))
                        throw new InvalidOperationException($"FK {fk.Nombre}: referencia fuera del conjunto aislado; revisar el SQL fuente.");
                    var referencia = $"REFERENCES {Q(esquema)}.{Q(fk.TablaDestino)}";
                    var patron = @"REFERENCES\s+(?:" + Regex.Escape(Q(origen)) + "|" + Regex.Escape(origen) + @")\.(?:" +
                        Regex.Escape(Q(fk.TablaDestino)) + "|" + Regex.Escape(fk.TablaDestino) + @")(?=\s|\()";
                    if (!Regex.IsMatch(fk.Definicion, patron))
                        throw new InvalidOperationException($"No se pudo redirigir la FK {fk.Nombre} al esquema aislado.");
                    var definicion = Regex.Replace(fk.Definicion, patron, referencia);
                    await fixture.EjecutarAsync($"ALTER TABLE {Q(esquema)}.{Q(tabla)} ADD CONSTRAINT {Q(fk.Nombre)} {definicion}");
                }
            }

            // Rol/Sucursal no tienen identity: son claves de catálogos declaradas
            // ValueGeneratedNever. Copiar sus valores reales evita inventar IDs.
            foreach (var tabla in new[] { "rol", "sucursal" })
                await fixture.EjecutarAsync($"INSERT INTO {Q(esquema)}.{Q(tabla)} SELECT * FROM {Q(origen)}.{Q(tabla)}");

            salida.WriteLine($"Proveedor real: Npgsql; base exclusiva: {config.Database}; plantilla: {origen}; esquema aislado: {esquema}.");
            salida.WriteLine("Tablas verificadas y clonadas: " + string.Join(", ", tablas));
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// LIKE ... INCLUDING ALL genera nombres nuevos para los índices (y las restricciones
    /// UNIQUE que los respaldan). El manejador global reconoce las violaciones por nombre,
    /// así que la copia tiene que llevar los del origen. Se aparean por tabla, unicidad y
    /// definición; ALTER INDEX también renombra la restricción asociada.
    /// </summary>
    private async Task RestaurarNombresDeIndicesAsync(string origen, string[] tablas)
    {
        async Task<List<(string Tabla, string Nombre, string Firma)>> IndicesAsync(string esquema)
        {
            var indices = new List<(string, string, string)>();
            await using var comando = new NpgsqlCommand("""
                SELECT t.relname, ix.relname,
                       i.indisunique::text || ' ' || regexp_replace(pg_get_indexdef(i.indexrelid), '^.* USING ', '')
                FROM pg_index i JOIN pg_class ix ON ix.oid = i.indexrelid
                JOIN pg_class t ON t.oid = i.indrelid JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = @schema AND t.relname = ANY(@tablas)
                """, _administracion);
            comando.Parameters.AddWithValue("schema", esquema);
            comando.Parameters.AddWithValue("tablas", tablas);
            await using var reader = await comando.ExecuteReaderAsync();
            while (await reader.ReadAsync()) indices.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
            return indices;
        }

        var originales = await IndicesAsync(origen);
        var copiados = await IndicesAsync(_esquema);
        foreach (var original in originales)
        {
            var candidatos = copiados.Where(c => c.Tabla == original.Tabla && c.Firma == original.Firma).ToList();
            if (candidatos.Count != 1 || originales.Count(o => o.Tabla == original.Tabla && o.Firma == original.Firma) != 1)
                throw new InvalidOperationException($"No se pudo aparear sin ambigüedad el índice {original.Tabla}.{original.Nombre} en la copia.");
            if (candidatos[0].Nombre != original.Nombre)
                await EjecutarAsync($"ALTER INDEX {Q(_esquema)}.{Q(candidatos[0].Nombre)} RENAME TO {Q(original.Nombre)}");
        }
    }

    /// <summary>
    /// Siembra un usuario real en el esquema aislado (rol y sucursal copiados de los
    /// catálogos) y devuelve el Id que generó la base. Lo exigen las FK de auditoría.
    /// </summary>
    public async Task<int> CrearUsuarioAsync(string nombreUsuario = "auditor")
    {
        await using var contexto = CrearContexto();
        var usuario = new Usuario
        {
            NombreUsuario = nombreUsuario,
            HashContrasena = "hash-de-prueba",
            RolId = await contexto.Rol.OrderBy(r => r.Id).Select(r => r.Id).FirstAsync(),
            SucursalId = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync(),
            Activo = true
        };
        contexto.Usuario.Add(usuario);
        await contexto.SaveChangesAsync();
        return usuario.Id;
    }

    /// <summary>
    /// Registra algo que usa el esquema (una fábrica de WithWebHostBuilder, por ejemplo)
    /// para liberarlo junto con él y no recién al final de la clase de tests.
    /// </summary>
    public T Liberar<T>(T recurso) where T : IAsyncDisposable => recurso;

    private async Task ValidarTablaAsync(string origen, IEntityType entidad)
    {
        var tabla = entidad.GetTableName()!;
        var columnas = new Dictionary<string, (string Identity, string Tipo, bool NoNula)>();
        await using (var command = new NpgsqlCommand("""
            SELECT a.attname, a.attidentity::text, format_type(a.atttypid, a.atttypmod), a.attnotnull
            FROM pg_attribute a JOIN pg_class t ON t.oid = a.attrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE n.nspname = @schema AND t.relname = @table AND a.attnum > 0 AND NOT a.attisdropped
            """, _administracion))
        {
            command.Parameters.AddWithValue("schema", origen);
            command.Parameters.AddWithValue("table", tabla);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                columnas.Add(reader.GetString(0), (reader.GetString(1), reader.GetString(2), reader.GetBoolean(3)));
        }
        var store = StoreObjectIdentifier.Table(tabla, entidad.GetSchema());
        foreach (var propiedad in entidad.GetProperties())
        {
            var columna = propiedad.GetColumnName(store)!;
            if (!columnas.TryGetValue(columna, out var real))
                throw new InvalidOperationException($"Falta {origen}.{tabla}.{columna}: aplicar/revisar el SQL del PR #110 en la base de test.");
            if (propiedad.FindAnnotation("Npgsql:ValueGenerationStrategy")?.Value?.ToString() == "IdentityAlwaysColumn" && real.Identity != "a")
                throw new InvalidOperationException($"{tabla}.{columna} no es GENERATED ALWAYS AS IDENTITY en la plantilla real.");
            if (propiedad.ValueGenerated == ValueGenerated.Never && real.Identity.Length > 0)
                throw new InvalidOperationException($"{tabla}.{columna} tiene identity, pero EF la declara ValueGeneratedNever: revisar el scaffold.");
            var esperado = propiedad.GetRelationalTypeMapping().StoreType;
            if (NormalizarTipo(real.Tipo) != NormalizarTipo(esperado) || real.NoNula == propiedad.IsNullable)
                throw new InvalidOperationException($"Tipo/nulabilidad de {tabla}.{columna} no coincide con el mapeo EF: real {real.Tipo}, esperado {esperado}.");
        }

        // Revisar que el origen conserve las relaciones mapeadas, no limitarse
        // a copiar una plantilla a la que podrían faltarle las restricciones.
        var fksReales = new HashSet<string>();
        await using (var fks = new NpgsqlCommand("""
            SELECT c.conname FROM pg_constraint c JOIN pg_class t ON t.oid = c.conrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE c.contype = 'f' AND n.nspname = @schema AND t.relname = @table
            """, _administracion))
        {
            fks.Parameters.AddWithValue("schema", origen);
            fks.Parameters.AddWithValue("table", tabla);
            await using var reader = await fks.ExecuteReaderAsync();
            while (await reader.ReadAsync()) fksReales.Add(reader.GetString(0));
        }
        foreach (var fk in entidad.GetForeignKeys())
            if (!fksReales.Contains(fk.GetConstraintName()!))
                throw new InvalidOperationException($"Falta FK {fk.GetConstraintName()} en {origen}.{tabla}; revisar el SQL real.");

        // No ejecutar triggers de origen que puedan escribir fuera de la copia.
        await using var seguridad = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM pg_trigger g JOIN pg_class t ON t.oid = g.tgrelid
                JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = @schema AND t.relname = @table AND NOT g.tgisinternal)
                OR EXISTS (SELECT 1 FROM pg_attrdef d JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum
                JOIN pg_class t ON t.oid = d.adrelid JOIN pg_namespace n ON n.oid = t.relnamespace
                WHERE n.nspname = @schema AND t.relname = @table AND a.attidentity = ''
                    AND pg_get_expr(d.adbin, d.adrelid) LIKE '%nextval(%')
            """, _administracion);
        seguridad.Parameters.AddWithValue("schema", origen);
        seguridad.Parameters.AddWithValue("table", tabla);
        if ((bool)(await seguridad.ExecuteScalarAsync())!)
            throw new InvalidOperationException($"{tabla} tiene triggers o secuencias serial externas: se necesita un script de fixture revisado antes de ejecutarlos.");

        // Una FK globalmente única contradice D10; no ocultarla en la copia.
        if (tabla.EndsWith("_historial_baja", StringComparison.Ordinal))
        {
            var clave = tabla.Replace("_historial_baja", "_id");
            await using var cardinalidad = new NpgsqlCommand("""
                SELECT EXISTS (SELECT 1 FROM pg_index i JOIN pg_class t ON t.oid = i.indrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(i.indkey)
                    WHERE n.nspname = @schema AND t.relname = @table AND i.indisunique
                        AND i.indpred IS NULL AND i.indnkeyatts = 1 AND a.attname = @column)
                """, _administracion);
            cardinalidad.Parameters.AddWithValue("schema", origen);
            cardinalidad.Parameters.AddWithValue("table", tabla);
            cardinalidad.Parameters.AddWithValue("column", clave);
            if ((bool)(await cardinalidad.ExecuteScalarAsync())!)
                throw new InvalidOperationException($"{tabla} limita todo el historial a 1:1: revisar el esquema real D10, sin relajar restricciones desde el test.");

            await using var abierta = new NpgsqlCommand("""
                SELECT EXISTS (SELECT 1 FROM pg_index i JOIN pg_class t ON t.oid = i.indrelid
                    JOIN pg_namespace n ON n.oid = t.relnamespace
                    JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(i.indkey)
                    WHERE n.nspname = @schema AND t.relname = @table AND i.indisunique
                        AND i.indpred IS NOT NULL AND i.indnkeyatts = 1 AND a.attname = @column
                        AND replace(replace(pg_get_expr(i.indpred, i.indrelid), '(', ''), ')', '') = 'fecha_reactivacion IS NULL')
                """, _administracion);
            abierta.Parameters.AddWithValue("schema", origen);
            abierta.Parameters.AddWithValue("table", tabla);
            abierta.Parameters.AddWithValue("column", clave);
            if (!(bool)(await abierta.ExecuteScalarAsync())!)
                throw new InvalidOperationException($"Falta el índice único de baja abierta en {origen}.{tabla}.");
        }
    }

    public static void ValidarBase(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            !(nombre.StartsWith("test_", StringComparison.OrdinalIgnoreCase) || nombre.EndsWith("_test", StringComparison.OrdinalIgnoreCase)) ||
            Regex.IsMatch(nombre, "principal|production|produccion|(?:^|_)prod(?:_|$)", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("Solo se permite una base exclusiva test_* o *_test; principal/producción se rechazan.");
    }

    private static string NormalizarTipo(string tipo) => tipo.ToLowerInvariant()
        .Replace("character varying", "varchar").Replace("timestamp with time zone", "timestamptz")
        .Replace("timestamp without time zone", "timestamp").Replace(" ", "");
    private static string Q(string identificador) => '"' + identificador.Replace("\"", "\"\"") + '"';

    private async Task EjecutarAsync(string sql)
    {
        await using var command = new NpgsqlCommand(sql, _administracion);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_creado && Regex.IsMatch(_esquema, "^pr110_test_[0-9a-f]{32}$") &&
                _administracion.State == System.Data.ConnectionState.Open)
            {
                await EjecutarAsync($"DROP SCHEMA {Q(_esquema)} CASCADE");
                _creado = false;
            }
        }
        finally { await _administracion.DisposeAsync(); }
    }
}

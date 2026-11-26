using LaCentral.Tests.Infraestructura;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// backend/db/base_completa.sql es el esquema v3.0, histórico: lo vigente es él más los scripts de
/// backend/db/rescaffold/. Lo que sí tiene que coincidir con las bases vivas (public de lacentral_test)
/// son el CHECK de baja de la D10 y la PK de las cuatro tablas con baja (aviso de Javi, 26/11/2026);
/// el resto del desfase es deuda (ver ESTADO del Sprint 7). Se corre solo la parte de tablas de la PARTE A, dentro de un
/// esquema descartable y de una transacción que se revierte: el archivo empieza con
/// DROP SCHEMA public CASCADE, y eso no se ejecuta nunca desde acá.
/// </summary>
[Trait("Esquema", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class BaseCompletaPostgreSqlTests
{
    private const string Desde = "CREATE EXTENSION IF NOT EXISTS pg_trgm;";
    private const string Hasta = "CREATE SCHEMA staging;";
    private static readonly string[] TablasConBaja = ["usuario", "cliente", "proveedor", "articulo"];
    private readonly ITestOutputHelper _salida;
    public BaseCompletaPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static string LeerTablas()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var ruta = Path.Combine(dir.FullName, "backend", "db", "base_completa.sql");
            if (!File.Exists(ruta)) continue;
            var texto = File.ReadAllText(ruta);
            var inicio = texto.IndexOf(Desde, StringComparison.Ordinal);
            var fin = texto.IndexOf(Hasta, StringComparison.Ordinal);
            if (inicio < 0 || fin < inicio)
                throw new InvalidOperationException($"base_completa.sql ya no tiene «{Desde}» seguido de «{Hasta}».");
            var tablas = texto[(inicio + Desde.Length)..fin];
            // Guarda: lo que se corre no puede borrar ni salir del esquema descartable.
            foreach (var prohibido in new[] { "DROP ", "TRUNCATE", "SCHEMA", "public.", "search_path" })
                if (tablas.Contains(prohibido, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"La parte de tablas de base_completa.sql contiene «{prohibido}»: no se ejecuta.");
            return tablas;
        }
        throw new FileNotFoundException("No encontré backend/db/base_completa.sql.");
    }

    private static async Task<Dictionary<string, string>> RestriccionesAsync(NpgsqlConnection conexion, string esquema)
    {
        // Cada esquema se lee con search_path en él solo: así las FK salen sin calificar en los dos.
        await using (var ruta = new NpgsqlCommand($"SET LOCAL search_path = \"{esquema}\", pg_catalog", conexion))
            await ruta.ExecuteNonQueryAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT t.relname || '.' || c.conname, pg_get_constraintdef(c.oid)
              FROM pg_constraint c
              JOIN pg_class t ON t.oid = c.conrelid
              JOIN pg_namespace n ON n.oid = t.relnamespace
             WHERE n.nspname = @esquema AND t.relname = ANY(@tablas)
               AND (c.contype = 'p' OR c.conname = t.relname || '_baja_coherente')
            """, conexion);
        comando.Parameters.AddWithValue("esquema", esquema);
        comando.Parameters.AddWithValue("tablas", TablasConBaja);
        var resultado = new Dictionary<string, string>();
        await using var lector = await comando.ExecuteReaderAsync();
        while (await lector.ReadAsync()) resultado[lector.GetString(0)] = lector.GetString(1);
        return resultado;
    }

    [PostgreSqlFact]
    public async Task ElCheckDeBajaYLaPkDeLasTablasConBaja_SonLosDePublicDeLacentralTest()
    {
        var config = new NpgsqlConnectionStringBuilder(PostgreSqlPruebas.ConexionRequerida);
        PostgreSqlPruebas.ValidarBase(config.Database);
        var esquema = "bc_prueba_" + Guid.NewGuid().ToString("N");
        var tablas = LeerTablas();

        await using var conexion = new NpgsqlConnection(config.ConnectionString);
        await conexion.OpenAsync();
        // DDL transaccional: con el ROLLBACK (o si se corta la conexión) el esquema no queda.
        await using var transaccion = await conexion.BeginTransactionAsync();
        await using (var crear = new NpgsqlCommand($"CREATE SCHEMA \"{esquema}\"; SET LOCAL search_path = \"{esquema}\", public", conexion))
            await crear.ExecuteNonQueryAsync();
        await using (var armar = new NpgsqlCommand(tablas, conexion) { CommandTimeout = 120 })
            await armar.ExecuteNonQueryAsync();

        var archivo = await RestriccionesAsync(conexion, esquema);
        var vivas = await RestriccionesAsync(conexion, "public");
        await transaccion.RollbackAsync();

        var diferencias = vivas.Where(v => !archivo.TryGetValue(v.Key, out var d) || d != v.Value)
                .Select(v => $"en public: {v.Key} {v.Value}" + (archivo.TryGetValue(v.Key, out var d) ? $"\n   en el archivo: {d}" : " (falta en el archivo)"))
            .Concat(archivo.Where(a => !vivas.ContainsKey(a.Key)).Select(a => $"sobra en el archivo: {a.Key} {a.Value}"))
            .Order().ToList();
        foreach (var linea in diferencias) _salida.WriteLine(linea);
        // Sin esto, una consulta que no encuentra nada en ninguno de los dos lados daría verde.
        Assert.Equal(2 * TablasConBaja.Length, vivas.Count);
        Assert.True(diferencias.Count == 0, $"{diferencias.Count} restricciones distintas:\n" + string.Join("\n", diferencias));
    }
}

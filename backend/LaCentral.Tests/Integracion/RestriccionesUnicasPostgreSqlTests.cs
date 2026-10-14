using LaCentral.Api.Middleware;
using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// El 409 del manejador global depende de nombres de restricción escritos a mano.
/// Si uno no existe en el esquema real, esa violación sale como 500 y nadie se entera.
/// </summary>
[Trait("Api", "PostgreSQL")]
public class RestriccionesUnicasPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public RestriccionesUnicasPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task CadaRestriccionReconocida_ExisteComoIndiceUnicoEnElEsquemaReal()
    {
        var config = new NpgsqlConnectionStringBuilder(PostgreSqlPruebas.ConexionRequerida);
        PostgreSqlPruebas.ValidarBase(config.Database);
        var esquema = Environment.GetEnvironmentVariable("LA_CENTRAL_TEST_SCHEMA") ?? "public";

        // Solo lectura de catálogo sobre la plantilla real: no crea ni modifica nada.
        await using var conexion = new NpgsqlConnection(config.ConnectionString);
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("""
            SELECT ix.relname FROM pg_index i
            JOIN pg_class ix ON ix.oid = i.indexrelid
            JOIN pg_namespace n ON n.oid = ix.relnamespace
            WHERE i.indisunique AND n.nspname = @esquema
            """, conexion);
        comando.Parameters.AddWithValue("esquema", esquema);
        var unicos = new HashSet<string>();
        await using (var lector = await comando.ExecuteReaderAsync())
            while (await lector.ReadAsync()) unicos.Add(lector.GetString(0));

        var faltantes = ManejadorExcepcionesGlobal.RestriccionesUnicasReconocidas.Keys
            .Where(nombre => !unicos.Contains(nombre)).ToList();
        _salida.WriteLine($"Esquema {esquema}: {unicos.Count} índices únicos.");
        Assert.Empty(faltantes);
    }

    [PostgreSqlFact]
    public async Task DuplicadoReal_ProduceUnaExcepcionQueElManejadorReconoce()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var primero = fixture.CrearContexto())
        {
            primero.Proveedor.Add(new Proveedor { Codigo = "PRV-DUP", RazonSocial = "Primero", Activo = true });
            await primero.SaveChangesAsync();
        }

        await using var segundo = fixture.CrearContexto();
        segundo.Proveedor.Add(new Proveedor { Codigo = "PRV-DUP", RazonSocial = "Segundo", Activo = true });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());

        Assert.Equal("Ya existe un proveedor registrado con ese código.", ManejadorExcepcionesGlobal.MensajeDeConflicto(error));
    }

    // El fixture clona con LIKE ... INCLUDING ALL, que inventa nombres nuevos para los
    // índices. Si el esquema aislado no conserva los originales, la 23505 trae otro
    // nombre y el manejador ve un 500 donde producción vería un 409.
    [PostgreSqlFact]
    public async Task EsquemaAislado_ConservaLosNombresDeIndicesDelOrigen()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var contexto = fixture.CrearContexto();
        var tablas = contexto.Model.GetEntityTypes().Select(e => e.GetTableName()).OfType<string>().Distinct().ToArray();
        var origen = Environment.GetEnvironmentVariable("LA_CENTRAL_TEST_SCHEMA") ?? "public";

        var conexion = (NpgsqlConnection)contexto.Database.GetDbConnection();
        await conexion.OpenAsync();
        async Task<string[]> IndicesAsync(string? esquema)
        {
            await using var comando = new NpgsqlCommand("""
                SELECT tablename || '.' || indexname FROM pg_indexes
                WHERE schemaname = coalesce(@esquema, current_schema()) AND tablename = ANY(@tablas)
                ORDER BY 1
                """, conexion);
            comando.Parameters.AddWithValue("esquema", NpgsqlTypes.NpgsqlDbType.Text, (object?)esquema ?? DBNull.Value);
            comando.Parameters.AddWithValue("tablas", tablas);
            var nombres = new List<string>();
            await using var lector = await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync()) nombres.Add(lector.GetString(0));
            return nombres.ToArray();
        }

        var enOrigen = await IndicesAsync(origen);
        var enCopia = await IndicesAsync(null);

        Assert.NotEmpty(enOrigen);
        Assert.Equal(enOrigen, enCopia);
    }

    [PostgreSqlFact]
    public async Task BajaAbiertaDuplicada_ElManejadorLaReconoceComoConflicto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var cliente = new Cliente { Codigo = "CLI-HIST", RazonSocial = "Cliente", Activo = true };
        await using (var setup = fixture.CrearContexto())
        {
            setup.Cliente.Add(cliente);
            setup.ClienteHistorialBaja.Add(new ClienteHistorialBaja { Cliente = cliente, Motivo = "Primera", FechaBaja = DateTime.UtcNow });
            await setup.SaveChangesAsync();
        }

        await using var segundo = fixture.CrearContexto();
        segundo.ClienteHistorialBaja.Add(new ClienteHistorialBaja { ClienteId = cliente.Id, Motivo = "Segunda", FechaBaja = DateTime.UtcNow });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => segundo.SaveChangesAsync());

        Assert.Equal("ux_cliente_hist_abierta", Assert.IsType<PostgresException>(error.InnerException).ConstraintName);
        Assert.Equal("El cliente ya tiene una baja abierta.", ManejadorExcepcionesGlobal.MensajeDeConflicto(error));
    }
}

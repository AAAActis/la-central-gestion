using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// El fixture no puede dejar rastros en la base compartida: ni un esquema huérfano si
/// la creación se corta a mitad de camino, ni conexiones idle en el pool del esquema
/// después de liberarlo. Las dos cosas juntas agotaban las ranuras de lacentral_tests
/// y la suite caía por 53300.
/// </summary>
public class PostgreSqlPruebasLimpiezaTests
{
    private readonly ITestOutputHelper _salida;
    public PostgreSqlPruebasLimpiezaTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task CreacionCortadaAMitadDeCamino_NoDejaElEsquemaHuerfano()
    {
        string? esquema = null;

        // Falla real: el servidor corta la conexión de administración (como una caída
        // de red) apenas creado el esquema, antes de clonar las tablas.
        await Assert.ThrowsAnyAsync<Exception>(() => PostgreSqlPruebas.CrearAsync(_salida, async (admin, creado) =>
        {
            esquema = creado;
            await using var corte = new NpgsqlCommand("SELECT pg_terminate_backend(pg_backend_pid())", admin);
            await corte.ExecuteNonQueryAsync();
        }));

        Assert.NotNull(esquema);
        try
        {
            Assert.False(await ExisteEsquemaAsync(esquema), $"Quedó huérfano el esquema {esquema}.");
        }
        finally
        {
            await BorrarSiQuedoAsync(esquema);
        }
    }

    [PostgreSqlFact]
    public async Task Liberado_NoQuedanConexionesDelPoolDeSuEsquema()
    {
        string esquema;
        await using (var fixture = await PostgreSqlPruebas.CrearAsync(_salida))
        {
            esquema = new NpgsqlConnectionStringBuilder(fixture.ConexionAislada).SearchPath!;
            await using var contexto = fixture.CrearContexto();
            Assert.True(await contexto.Rol.AnyAsync());
            Assert.True(await ConexionesDelEsquemaAsync(esquema) > 0, "La consulta tenía que abrir una conexión del pool.");
        }

        Assert.Equal(0, await ConexionesDelEsquemaAsync(esquema, esperarCero: true));
    }

    [PostgreSqlFact]
    public async Task Liberado_LiberaLoQueSeRegistroConEl()
    {
        var registrado = new Registro();
        await using (var fixture = await PostgreSqlPruebas.CrearAsync(_salida))
            Assert.Same(registrado, fixture.Liberar(registrado));

        Assert.True(registrado.Liberado);
    }

    private sealed class Registro : IAsyncDisposable
    {
        public bool Liberado { get; private set; }
        public ValueTask DisposeAsync() { Liberado = true; return ValueTask.CompletedTask; }
    }

    private static NpgsqlConnection ConexionSinPool() =>
        new(new NpgsqlConnectionStringBuilder(PostgreSqlPruebas.ConexionRequerida) { Pooling = false }.ConnectionString);

    private static async Task<bool> ExisteEsquemaAsync(string esquema)
    {
        await using var conexion = ConexionSinPool();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = @e)", conexion);
        comando.Parameters.AddWithValue("e", esquema);
        return (bool)(await comando.ExecuteScalarAsync())!;
    }

    /// <summary>Si el test queda en rojo, no deja el huérfano que acaba de detectar.</summary>
    private static async Task BorrarSiQuedoAsync(string esquema)
    {
        if (!Regex.IsMatch(esquema, "^pr110_test_[0-9a-f]{32}$")) return;
        await using var conexion = ConexionSinPool();
        await conexion.OpenAsync();
        await using var comando = new NpgsqlCommand($"DROP SCHEMA IF EXISTS \"{esquema}\" CASCADE", conexion);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Cuenta las sesiones con application_name = esquema. Un backend que se cierra tarda
    /// unos milisegundos en salir de pg_stat_activity: si se espera cero, se reintenta.
    /// </summary>
    private static async Task<long> ConexionesDelEsquemaAsync(string esquema, bool esperarCero = false)
    {
        long cantidad = 0;
        for (var intento = 0; intento < (esperarCero ? 20 : 1); intento++)
        {
            await using var conexion = ConexionSinPool();
            await conexion.OpenAsync();
            await using var comando = new NpgsqlCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE application_name = @e", conexion);
            comando.Parameters.AddWithValue("e", esquema);
            cantidad = (long)(await comando.ExecuteScalarAsync())!;
            if (cantidad == 0) break;
            await Task.Delay(150);
        }
        return cantidad;
    }
}

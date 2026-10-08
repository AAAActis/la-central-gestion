using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.UseCases.Articulos.Dtos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LaCentral.Tests.Articulos;

public sealed class PostgresBusquedaFactAttribute : FactAttribute
{
    public const string VariableConexion = "LA_CENTRAL_TEST_POSTGRES";

    public PostgresBusquedaFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(VariableConexion)))
            Skip = "Falta LA_CENTRAL_TEST_POSTGRES: pendiente de PostgreSQL exclusivo por Tailscale.";
    }
}

public class ArticuloBusquedaPostgreSqlTests
{
    [PostgresBusquedaFact]
    public async Task BusquedaReal_NormalizacionAlternativasPaginacionYUnicidadD2()
    {
        var conexion = Environment.GetEnvironmentVariable(PostgresBusquedaFactAttribute.VariableConexion)!;
        var configuracion = new NpgsqlConnectionStringBuilder(conexion);
        // Evita que un test escriba en la base compartida de demostración.
        if (configuracion.Database is not string baseTest ||
            !(baseTest.EndsWith("_test", StringComparison.OrdinalIgnoreCase) ||
              baseTest.StartsWith("test_", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("La base exclusiva de pruebas debe llamarse test_* o *_test.");

        var esquema = "art02_test_" + Guid.NewGuid().ToString("N");
        configuracion.SearchPath = esquema;
        await using var connection = new NpgsqlConnection(configuracion.ConnectionString);
        await connection.OpenAsync();
        var creado = false;
        try
        {
            await Ejecutar(connection, $"CREATE SCHEMA {esquema}");
            creado = true;
            // Esquema mínimo de lectura, no reemplaza el SQL de D1 ni el scaffold.
            await Ejecutar(connection, """
                CREATE TABLE articulo (
                    id integer PRIMARY KEY, codigo_interno text UNIQUE NOT NULL,
                    nombre text NOT NULL, activo boolean NOT NULL);
                CREATE TABLE articulo_codigo_alternativo (
                    id integer PRIMARY KEY, articulo_id integer REFERENCES articulo(id),
                    proveedor_id integer NOT NULL, codigo text UNIQUE NOT NULL,
                    CONSTRAINT un_codigo_por_proveedor UNIQUE (articulo_id, proveedor_id));
                INSERT INTO articulo
                    SELECT n, 'ART-' || lpad(n::text, 6, '0'),
                           'Bujía NGK pequeña modelo ' || lpad(n::text, 2, '0'), n <> 40
                    FROM generate_series(1, 40) n;
                UPDATE articulo SET codigo_interno = ' GN-01 ' WHERE id = 3;
                INSERT INTO articulo_codigo_alternativo VALUES
                    (1, 1, 1, 'gn-01'), (2, 2, 1, ' GN-01 ');
                """);
            var options = new DbContextOptionsBuilder<LaCentralDbContext>()
                .UseNpgsql(connection).Options;
            await using var context = new LaCentralDbContext(options);
            var repo = new ArticuloRepositorio(context);

            var pagina = await repo.BuscarAsync(new("gn-01*bujia ngk", TamanoPagina: 3));
            Assert.Equal(39, pagina.Total);
            Assert.Equal(new[] { 3, 1, 2 }, pagina.Articulos.Select(a => a.Id));
            Assert.Equal(new[] { 1, 2, 3 }, Assert.Single(pagina.Conflictos).ArticuloIds);

            var siguiente = await repo.BuscarAsync(new("bujia pequeña", Pagina: 2, TamanoPagina: 3));
            Assert.Equal(new[] { 4, 5, 6 }, siguiente.Articulos.Select(a => a.Id));
            Assert.Equal(40, (await repo.BuscarAsync(new("bujia", IncluirInactivos: true))).Total);
            Assert.Empty((await repo.BuscarAsync(new("%_"))).Articulos);
            Assert.Empty((await repo.BuscarAsync(new("no existe"))).Articulos);

            var duplicado = await Assert.ThrowsAsync<PostgresException>(() => Ejecutar(connection,
                "INSERT INTO articulo_codigo_alternativo VALUES (3, 1, 1, 'OTRO-CODIGO')"));
            Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicado.SqlState);
            Assert.Equal("un_codigo_por_proveedor", duplicado.ConstraintName);
        }
        finally
        {
            // Solo el esquema recién generado por este test. No se limpia public.
            if (creado && esquema.StartsWith("art02_test_", StringComparison.Ordinal) &&
                Guid.TryParseExact(esquema[11..], "N", out _))
                await Ejecutar(connection, $"DROP SCHEMA {esquema} CASCADE");
        }
    }

    private static async Task Ejecutar(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

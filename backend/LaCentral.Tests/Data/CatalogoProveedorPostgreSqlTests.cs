using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

/// <summary>
/// HU-CMP-08 (055 v2.0, D-S7-01): n8n escribe el catálogo de cada proveedor directo en la base, con las cuatro
/// sentencias de <c>backend/db/n8n/</c>, que son las mismas que corren acá. Cada resultado se relee con un
/// contexto nuevo.
/// </summary>
[Trait("Base", "PostgreSQL")]
public class CatalogoProveedorPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public CatalogoProveedorPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task CA001_Reingestar_NoDuplica_ActualizaElRenglon_YConservaLaVinculacion()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-1");
        int articuloId;
        await using (var setup = fixture.CrearContexto())
        {
            var articulo = new Articulo { CodigoInterno = "SCR-ART-1", Nombre = "Pastilla de freno", Activo = true };
            setup.Articulo.Add(articulo);
            setup.ArticuloCodigoAlternativo.Add(new ArticuloCodigoAlternativo { Articulo = articulo, ProveedorId = proveedorId, Codigo = "X-1" });
            await setup.SaveChangesAsync();
            articuloId = articulo.Id;
        }

        await using var n8n = await N8n.ConectarAsync(fixture);
        var primera = await n8n.AbrirAsync(proveedorId);
        Assert.NotNull(await n8n.RenglonAsync(primera, " X-1 ", "Pastilla delantera", 100m));
        Assert.NotNull(await n8n.RenglonAsync(primera, "X-1", "Pastilla delantera", 101m)); // página reenviada
        await n8n.CerrarAsync(primera, "SUCCESS");
        var segunda = await n8n.AbrirAsync(proveedorId);
        Assert.NotNull(await n8n.RenglonAsync(segunda, "X-1", "Pastilla delantera cerámica", 120m));
        await n8n.CerrarAsync(segunda, "SUCCESS");

        await using var lectura = fixture.CrearContexto();
        var renglon = Assert.Single(await lectura.ProveedorArticulo.Where(p => p.ProveedorId == proveedorId).ToListAsync());
        Assert.Equal(("X-1", "Pastilla delantera cerámica", 120m, true, segunda),
            (renglon.Codigo, renglon.Nombre, renglon.PrecioNeto, renglon.Activo, renglon.UltimaCorridaId));
        var historial = await lectura.ProveedorArticuloHistorial.Where(h => h.ProveedorArticuloId == renglon.Id)
            .OrderBy(h => h.CorridaId).Select(h => new { h.CorridaId, h.PrecioNeto }).ToListAsync();
        Assert.Equal(new[] { (primera, (decimal?)101m), (segunda, (decimal?)120m) },
            historial.Select(h => (h.CorridaId, h.PrecioNeto)).ToArray());
        // La vinculación es por proveedor y código: sigue encontrando el artículo propio.
        Assert.Equal(articuloId, await lectura.ArticuloCodigoAlternativo
            .Where(v => v.ProveedorId == renglon.ProveedorId && v.Codigo == renglon.Codigo).Select(v => v.ArticuloId).SingleAsync());
    }

    [PostgreSqlFact]
    public async Task CA002_CadaCorridaQuedaRegistrada_ConSuEstadoSuTotalYSusItems_YDosNoSePisan()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-2");
        await using var n8n = await N8n.ConectarAsync(fixture);

        var exitosa = await n8n.AbrirAsync(proveedorId, "ejec-41");
        var pisada = await Assert.ThrowsAsync<PostgresException>(() => n8n.AbrirAsync(proveedorId, "ejec-42"));
        Assert.Equal(("23505", "ux_scrape_corrida_en_curso"), (pisada.SqlState, pisada.ConstraintName));
        Assert.Equal(exitosa, await n8n.EscalarAsync("02_total_informado.sql", exitosa, 2));
        await n8n.RenglonAsync(exitosa, "A", "Uno", 10m);
        await n8n.RenglonAsync(exitosa, "B", "Dos", 20m);
        Assert.Equal((exitosa, 2, 0L), await n8n.CerrarAsync(exitosa, "SUCCESS"));
        Assert.Null(await n8n.CerrarFilaAsync(exitosa, "FAILED", "tarde")); // ya cerrada: no cambia
        Assert.Null(await n8n.RenglonAsync(exitosa, "C", "Tres", 30m));    // ya cerrada: no escribe

        var fallida = await n8n.AbrirAsync(proveedorId, "ejec-43");
        await n8n.RenglonAsync(fallida, "A", "Uno", 11m);
        Assert.Equal((fallida, 1, 0L), await n8n.CerrarAsync(fallida, "FAILED", "login rechazado"));

        await using var lectura = fixture.CrearContexto();
        var corridas = await lectura.ScrapeCorrida.Where(c => c.ProveedorId == proveedorId).OrderBy(c => c.Id).ToListAsync();
        Assert.Equal(new (int, string, string, int?, int?, string?)[] { (exitosa, "SUCCESS", "ejec-41", 2, 2, null), (fallida, "FAILED", "ejec-43", null, 1, "login rechazado") },
            corridas.Select(c => (c.Id, c.Estado, c.EjecucionN8n!, c.TotalInformado, c.CantidadItems, c.MensajeError)).ToArray());
        Assert.All(corridas, c => Assert.True(c.Fin >= c.Inicio));
        Assert.False(await lectura.ProveedorArticulo.AnyAsync(p => p.Codigo == "C"));
    }

    [PostgreSqlFact]
    public async Task CA003_LoQueNoVino_QuedaVencidoSinBorrarse_SoloTrasUnaCorridaExitosa_YVuelveSiReaparece()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-3");
        var otroId = await CrearProveedorAsync(fixture, "SCR-3B");
        await using var n8n = await N8n.ConectarAsync(fixture);

        async Task<int> Corrida(int proveedor, string estado, params string[] codigos)
        {
            var id = await n8n.AbrirAsync(proveedor);
            foreach (var codigo in codigos) await n8n.RenglonAsync(id, codigo, "Renglón " + codigo, 50m);
            await n8n.CerrarAsync(id, estado);
            return id;
        }
        async Task<Dictionary<string, bool>> Activos()
        {
            await using var lectura = fixture.CrearContexto();
            return await lectura.ProveedorArticulo.OrderBy(p => p.Codigo).ToDictionaryAsync(p => p.Codigo, p => p.Activo);
        }

        await Corrida(proveedorId, "SUCCESS", "A", "B");
        await Corrida(otroId, "SUCCESS", "Z");
        await Corrida(proveedorId, "FAILED", "A");
        Assert.Equal(new Dictionary<string, bool> { ["A"] = true, ["B"] = true, ["Z"] = true }, await Activos());
        await Corrida(proveedorId, "SUCCESS");                         // sin ítems: no vence nada
        Assert.Equal(new Dictionary<string, bool> { ["A"] = true, ["B"] = true, ["Z"] = true }, await Activos());

        var sinB = await Corrida(proveedorId, "SUCCESS", "A");
        Assert.Equal(new Dictionary<string, bool> { ["A"] = true, ["B"] = false, ["Z"] = true }, await Activos());
        await using (var lectura = fixture.CrearContexto())
        {
            var b = await lectura.ProveedorArticulo.SingleAsync(p => p.Codigo == "B");
            Assert.Equal(1, await lectura.ProveedorArticuloHistorial.CountAsync(h => h.ProveedorArticuloId == b.Id));
            Assert.NotEqual(sinB, b.UltimaCorridaId);
        }

        await Corrida(proveedorId, "SUCCESS", "A", "B");
        Assert.Equal(new Dictionary<string, bool> { ["A"] = true, ["B"] = true, ["Z"] = true }, await Activos());
    }

    [PostgreSqlFact]
    public async Task CA004_ProveedorDadoDeBaja_NoAbreCorrida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-4", activo: false);
        var activoId = await CrearProveedorAsync(fixture, "SCR-4B");
        await using var n8n = await N8n.ConectarAsync(fixture);

        Assert.Null(await n8n.EscalarAsync("01_abrir_corrida.sql", proveedorId, "ejec-1"));
        Assert.NotNull(await n8n.EscalarAsync("01_abrir_corrida.sql", activoId, "ejec-2")); // el activo sí abre

        await using var lectura = fixture.CrearContexto();
        Assert.False(await lectura.ScrapeCorrida.AnyAsync(c => c.ProveedorId == proveedorId));
    }

    [PostgreSqlFact]
    public async Task LaBase_NoDejaMezclarUnRenglonConLaCorridaDeOtroProveedor()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-5");
        var otroId = await CrearProveedorAsync(fixture, "SCR-5B");
        await using var n8n = await N8n.ConectarAsync(fixture);
        var corridaDelOtro = await n8n.AbrirAsync(otroId);

        var error = await Assert.ThrowsAsync<PostgresException>(() => n8n.EjecutarAsync(
            "INSERT INTO proveedor_articulo (proveedor_id, codigo, nombre, ultima_corrida_id) VALUES ($1, 'M-1', 'Mezcla', $2)",
            proveedorId, corridaDelOtro));
        Assert.Equal(("23503", "proveedor_articulo_corrida_mismo_proveedor"), (error.SqlState, error.ConstraintName));
    }

    private static async Task<int> CrearProveedorAsync(PostgreSqlPruebas fixture, string codigo, bool activo = true)
    {
        await using var setup = fixture.CrearContexto();
        var proveedor = new Proveedor { Codigo = codigo, RazonSocial = "Proveedor " + codigo, Activo = activo };
        if (!activo) { proveedor.MotivoBaja = "Dejó de vender"; proveedor.FechaBaja = DateTime.UtcNow; }
        setup.Proveedor.Add(proveedor);
        await setup.SaveChangesAsync();
        return proveedor.Id;
    }

    /// <summary>Lo que hace un workflow de n8n: las sentencias de <c>backend/db/n8n/</c>, con parámetros posicionales.</summary>
    private sealed class N8n : IAsyncDisposable
    {
        private readonly LaCentralDbContext _contexto;
        private readonly NpgsqlConnection _conexion;
        private N8n(LaCentralDbContext contexto) { _contexto = contexto; _conexion = (NpgsqlConnection)contexto.Database.GetDbConnection(); }

        public static async Task<N8n> ConectarAsync(PostgreSqlPruebas fixture)
        {
            var n8n = new N8n(fixture.CrearContexto());
            await n8n._conexion.OpenAsync();
            return n8n;
        }

        public async Task<int> AbrirAsync(int proveedorId, string ejecucion = "ejec")
            => (int)(await EscalarAsync("01_abrir_corrida.sql", proveedorId, ejecucion)
                ?? throw new Xunit.Sdk.XunitException("01_abrir_corrida.sql no devolvió la corrida."));

        public Task<object?> RenglonAsync(int corrida, string codigo, string nombre, decimal precioNeto)
            => EscalarAsync("03_renglon.sql", corrida, codigo, null, null, nombre, null, "MARCA: Fras-le | EJE: delantero",
                "Fras-le", precioNeto, precioNeto * 1.3m, 4m, 21m, null, null);

        public async Task<(int, int, long)> CerrarAsync(int corrida, string estado, string? mensaje = null)
            => await CerrarFilaAsync(corrida, estado, mensaje)
                ?? throw new Xunit.Sdk.XunitException("04_cerrar_corrida.sql no devolvió la corrida.");

        public async Task<(int, int, long)?> CerrarFilaAsync(int corrida, string estado, string? mensaje)
        {
            await using var comando = Comando(Leer("04_cerrar_corrida.sql"), corrida, estado, mensaje);
            await using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() ? (lector.GetInt32(0), lector.GetInt32(1), lector.GetInt64(2)) : null;
        }

        public async Task<object?> EscalarAsync(string archivo, params object?[] valores)
        {
            await using var comando = Comando(Leer(archivo), valores);
            return await comando.ExecuteScalarAsync();
        }

        public async Task EjecutarAsync(string sql, params object?[] valores)
        {
            await using var comando = Comando(sql, valores);
            await comando.ExecuteNonQueryAsync();
        }

        private NpgsqlCommand Comando(string sql, params object?[] valores)
        {
            var comando = new NpgsqlCommand(sql, _conexion);
            foreach (var valor in valores) comando.Parameters.Add(new NpgsqlParameter { Value = valor ?? DBNull.Value });
            return comando;
        }

        private static string Leer(string archivo)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                var ruta = Path.Combine(dir.FullName, "db", "n8n", archivo);
                if (File.Exists(ruta)) return File.ReadAllText(ruta);
            }
            throw new FileNotFoundException($"No encontré backend/db/n8n/{archivo}.");
        }

        public async ValueTask DisposeAsync() => await _contexto.DisposeAsync();
    }
}

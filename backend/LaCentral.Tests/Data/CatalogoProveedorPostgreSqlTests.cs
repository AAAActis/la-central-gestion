using System.Globalization;
using System.Text.RegularExpressions;
using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Data;

/// <summary>
/// HU-CMP-08 (055 v2.0, D-S7-01): n8n escribe el catálogo de cada proveedor directo en la base, con las sentencias de
/// <c>docs/sprint07/CONTRATO-N8N.md</c>. Este test las lee de ahí, tal cual, y las corre como n8n: una sentencia por
/// nodo y los parámetros como texto. Cada resultado se relee con un contexto nuevo.
/// </summary>
[Trait("Base", "PostgreSQL")]
public class CatalogoProveedorPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public CatalogoProveedorPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [Fact]
    public void Contrato_TieneLasSieteSentencias_CadaUnaEnUnaSolaLinea()
    {
        var sentencias = N8n.Contrato();
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f", "g" }, sentencias.Keys.Order().ToArray());
        Assert.All(sentencias.Values, sql => Assert.DoesNotContain('\n', sql));
    }

    [PostgreSqlFact]
    public async Task CA001_Reingestar_NoDuplica_ActualizaElRenglonYSuHistorial_YConservaLaVinculacion()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-1");
        int articuloId;
        await using (var setup = fixture.CrearContexto())
        {
            var articulo = new Articulo { CodigoInterno = "SCR-ART-1", Nombre = "Pastilla de freno", Activo = true };
            setup.Articulo.Add(articulo);
            setup.ArticuloCodigoAlternativo.Add(new ArticuloCodigoAlternativo { Articulo = articulo, ProveedorId = proveedorId, Codigo = "C 000100 14 E" });
            await setup.SaveChangesAsync();
            articuloId = articulo.Id;
        }

        await using var n8n = await N8n.ConectarAsync(fixture);
        var primera = await n8n.AbrirAsync(proveedorId);
        await n8n.ProductoAsync(primera, Producto.Expoyer(" C 000100 14 E ", 100m, "Pastilla delantera"));
        await n8n.ProductoAsync(primera, Producto.Expoyer("C 000100 14 E", 101m, "Pastilla delantera")); // repetido en la corrida
        await n8n.CerrarAsync(primera, "SUCCESS");
        var segunda = await n8n.AbrirAsync(proveedorId);
        var ultimo = Producto.Expoyer("C 000100 14 E", 120m, "  Pastilla delantera cerámica ") with { EstadoStock = "NO_HAY", Activo = "false" };
        await n8n.ProductoAsync(segunda, ultimo);
        await n8n.CerrarAsync(segunda, "SUCCESS");

        await using var lectura = fixture.CrearContexto();
        var r = Assert.Single(await lectura.ProveedorArticulo.Where(p => p.ProveedorId == proveedorId).ToListAsync());
        Assert.Equal(("C 000100 14 E", "ext-120", (string?)null, "N-120 / M-120", "Pastilla delantera cerámica", "Desc 120", "EJE: 120", "Marca 120", "Rubro 120"),
            (r.Codigo, r.IdExterno, r.CodigoBarras, r.NumeroOriginal, r.Nombre, r.Descripcion, r.DetalleTecnico, r.Marca, r.Rubro));
        Assert.Equal(((decimal?)300m, (decimal?)120m, (decimal?)114m, "NO_HAY", (decimal?)null, (decimal?)null),
            (r.PrecioLista, r.PrecioNeto, r.PrecioPagoAnticipado, r.EstadoStock, r.Stock, r.AlicuotaIva));
        Assert.Equal(("https://img/120.jpg", new DateTime(2026, 11, 9, 0, 0, 0, DateTimeKind.Utc), false, false, segunda),
            (r.UrlImagen, r.FechaOrigen!.Value, r.Activo, r.Vencido, r.UltimaCorridaId));
        var historial = await lectura.ProveedorArticuloHistorial.Where(h => h.ProveedorArticuloId == r.Id).OrderBy(h => h.CorridaId)
            .Select(h => new { h.CorridaId, h.PrecioLista, h.PrecioNeto, h.PrecioPagoAnticipado, h.EstadoStock, h.Stock }).ToListAsync();
        Assert.Equal(new (int, decimal?, decimal?, decimal?, string?, decimal?)[]
            { (primera, 252.5m, 101m, 95.95m, "HAY", null), (segunda, 300m, 120m, 114m, "NO_HAY", null) },
            historial.Select(h => (h.CorridaId, h.PrecioLista, h.PrecioNeto, h.PrecioPagoAnticipado, h.EstadoStock, h.Stock)).ToArray());
        // La vinculación es por proveedor y código: sigue encontrando el artículo propio.
        Assert.Equal(articuloId, await lectura.ArticuloCodigoAlternativo
            .Where(v => v.ProveedorId == r.ProveedorId && v.Codigo == r.Codigo).Select(v => v.ArticuloId).SingleAsync());
    }

    [PostgreSqlFact]
    public async Task CA001_LoQueNoSeInforma_QuedaNull_YLaCantidadYLaAlicuotaSeGuardanCuandoVienen()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-1B");
        await using var n8n = await N8n.ConectarAsync(fixture);
        var corrida = await n8n.AbrirAsync(proveedorId);
        await n8n.ProductoAsync(corrida, new Producto("MIN-1", "Mínimo"));
        await n8n.ProductoAsync(corrida, new Producto("CON-1", "Con cantidad") { Stock = "7.5", AlicuotaIva = "10.5", CodigoBarras = "7790001" });
        await n8n.CerrarAsync(corrida, "SUCCESS");

        await using var lectura = fixture.CrearContexto();
        var filas = await lectura.ProveedorArticulo.Where(p => p.ProveedorId == proveedorId).OrderBy(p => p.Codigo).ToListAsync();
        Assert.Equal(new (string, decimal?, decimal?, string?, decimal?, string?, bool)[]
            { ("CON-1", 7.5m, 10.5m, "7790001", null, null, true), ("MIN-1", null, null, null, null, null, true) },
            filas.Select(f => (f.Codigo, f.Stock, f.AlicuotaIva, f.CodigoBarras, f.PrecioNeto, f.EstadoStock, f.Activo)).ToArray());
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
        Assert.Equal(exitosa, await n8n.EscalarAsync("g", exitosa, 2));
        var a = await n8n.ProductoAsync(exitosa, Producto.Expoyer("A", 10m));
        await n8n.ProductoAsync(exitosa, Producto.Expoyer("B", 20m));
        Assert.Equal((exitosa, 2), await n8n.CerrarAsync(exitosa, "SUCCESS"));
        // Cerrada: no cambia ni recibe nada.
        Assert.Null(await n8n.FilaAsync("d", exitosa, "FAILED", "tarde"));
        Assert.Null(await n8n.EscalarAsync("g", exitosa, 99));
        Assert.Null(await n8n.EscalarAsync("b", Producto.Expoyer("C", 30m).Parametros(exitosa)));
        Assert.Null(await n8n.EscalarAsync("c", exitosa, a));

        var fallida = await n8n.AbrirAsync(proveedorId, "ejec-43");
        await n8n.ProductoAsync(fallida, Producto.Expoyer("A", 11m));
        Assert.Equal((fallida, 1), await n8n.CerrarAsync(fallida, "FAILED", "login rechazado"));

        await using var lectura = fixture.CrearContexto();
        var corridas = await lectura.ScrapeCorrida.Where(c => c.ProveedorId == proveedorId).OrderBy(c => c.Id).ToListAsync();
        Assert.Equal(new (int, string, string?, int?, int?, string?)[] { (exitosa, "SUCCESS", "ejec-41", 2, 2, null), (fallida, "FAILED", "ejec-43", null, 1, "login rechazado") },
            corridas.Select(c => (c.Id, c.Estado, c.EjecucionN8n, c.TotalInformado, c.CantidadItems, c.MensajeError)).ToArray());
        Assert.All(corridas, c => Assert.True(c.Fin >= c.Inicio));
        Assert.False(await lectura.ProveedorArticulo.AnyAsync(p => p.Codigo == "C"));
        Assert.Equal(new[] { 10m }, await lectura.ProveedorArticuloHistorial.Where(h => h.CorridaId == exitosa && h.ProveedorArticuloId == a)
            .Select(h => h.PrecioNeto!.Value).ToArrayAsync());
    }

    [PostgreSqlFact]
    public async Task CA002_ElHistorial_SoloSeEscribeParaUnRenglonQueLeyoEsaCorrida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-2B");
        var otroId = await CrearProveedorAsync(fixture, "SCR-2C");
        await using var n8n = await N8n.ConectarAsync(fixture);
        var vieja = await n8n.AbrirAsync(proveedorId);
        var noLeido = await n8n.ProductoAsync(vieja, Producto.Expoyer("V", 10m));
        await n8n.CerrarAsync(vieja, "SUCCESS");
        var delOtro = await n8n.AbrirAsync(otroId);
        var ajeno = await n8n.ProductoAsync(delOtro, Producto.Expoyer("Z", 10m));
        var nueva = await n8n.AbrirAsync(proveedorId);

        Assert.Null(await n8n.EscalarAsync("c", nueva, noLeido)); // no lo leyó esta corrida
        Assert.Null(await n8n.EscalarAsync("c", nueva, ajeno));   // es de otro proveedor

        await using var lectura = fixture.CrearContexto();
        Assert.False(await lectura.ProveedorArticuloHistorial.AnyAsync(h => h.CorridaId == nueva));
    }

    [PostgreSqlFact]
    public async Task CA003_LoQueNoVino_QuedaVencidoSinBorrarse_SoloTrasUnaCorridaExitosa_YVuelveSiReaparece()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedorId = await CrearProveedorAsync(fixture, "SCR-3");
        var otroId = await CrearProveedorAsync(fixture, "SCR-3B");
        await using var n8n = await N8n.ConectarAsync(fixture);

        var vencidos = new List<long>();
        async Task<int> Corrida(int proveedor, string estado, params string[] codigos)
        {
            var id = await n8n.AbrirAsync(proveedor);
            foreach (var codigo in codigos) await n8n.ProductoAsync(id, Producto.Expoyer(codigo, 50m));
            vencidos.Add(await n8n.VencerAsync(id)); // antes de cerrar: no vence nada
            await n8n.CerrarAsync(id, estado);
            vencidos.Add(await n8n.VencerAsync(id));
            return id;
        }
        async Task<Dictionary<string, bool>> Vencidos()
        {
            await using var lectura = fixture.CrearContexto();
            return await lectura.ProveedorArticulo.OrderBy(p => p.Codigo).ToDictionaryAsync(p => p.Codigo, p => p.Vencido);
        }

        await Corrida(proveedorId, "SUCCESS", "A", "B");
        await Corrida(otroId, "SUCCESS", "Z");
        await Corrida(proveedorId, "FAILED", "A");
        await Corrida(proveedorId, "SUCCESS");                         // sin ítems: no vence nada
        Assert.Equal(new Dictionary<string, bool> { ["A"] = false, ["B"] = false, ["Z"] = false }, await Vencidos());

        var sinB = await Corrida(proveedorId, "SUCCESS", "A");
        Assert.Equal(new Dictionary<string, bool> { ["A"] = false, ["B"] = true, ["Z"] = false }, await Vencidos());
        await using (var lectura = fixture.CrearContexto())
        {
            var b = await lectura.ProveedorArticulo.SingleAsync(p => p.Codigo == "B");
            Assert.Equal((1, true), (await lectura.ProveedorArticuloHistorial.CountAsync(h => h.ProveedorArticuloId == b.Id), b.Activo));
            Assert.NotEqual(sinB, b.UltimaCorridaId);
        }
        await Corrida(proveedorId, "SUCCESS", "A");                    // B ya estaba vencido: no se cuenta otra vez
        await Corrida(proveedorId, "SUCCESS", "A", "B");
        Assert.Equal(new Dictionary<string, bool> { ["A"] = false, ["B"] = false, ["Z"] = false }, await Vencidos());
        Assert.Equal(new long[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 }, vencidos.ToArray());
    }

    [PostgreSqlFact]
    public async Task CA004_ProveedorDadoDeBaja_SeRechaza_SinAbrirCorrida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var bajaId = await CrearProveedorAsync(fixture, "SCR-4", activo: false);
        var activoId = await CrearProveedorAsync(fixture, "SCR-4B");
        await using var n8n = await N8n.ConectarAsync(fixture);

        Assert.Null(await n8n.EscalarAsync("f", bajaId));
        Assert.Null(await n8n.EscalarAsync("a", bajaId, "ejec-1"));
        Assert.Equal(activoId, await n8n.EscalarAsync("f", activoId));
        Assert.NotNull(await n8n.EscalarAsync("a", activoId, "ejec-2"));

        await using var lectura = fixture.CrearContexto();
        Assert.False(await lectura.ScrapeCorrida.AnyAsync(c => c.ProveedorId == bajaId));
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

    /// <summary>Un producto como lo arma el workflow: todo texto, vacío = desconocido.</summary>
    private sealed record Producto(string Codigo, string Nombre)
    {
        public string IdExterno { get; init; } = "";
        public string? CodigoBarras { get; init; }
        public string NumeroOriginal { get; init; } = "";
        public string Descripcion { get; init; } = "";
        public string DetalleTecnico { get; init; } = "";
        public string Marca { get; init; } = "";
        public string Rubro { get; init; } = "";
        public string PrecioLista { get; init; } = "";
        public string PrecioNeto { get; init; } = "";
        public string PrecioPagoAnticipado { get; init; } = "";
        public string EstadoStock { get; init; } = "";
        public string Stock { get; init; } = "";
        public string AlicuotaIva { get; init; } = "";
        public string UrlImagen { get; init; } = "";
        public string FechaOrigen { get; init; } = "";
        public string Activo { get; init; } = "";

        /// <summary>Como viene de Expoyer: sin cantidad ni alícuota, y cada campo cambia con el neto (así se nota una columna que el upsert no actualice).</summary>
        public static Producto Expoyer(string codigo, decimal neto, string? nombre = null)
        {
            var v = ((int)neto).ToString(CultureInfo.InvariantCulture);
            static string N(decimal d) => d.ToString(CultureInfo.InvariantCulture);
            return new Producto(codigo, nombre ?? "Renglón " + codigo)
            {
                IdExterno = "ext-" + v, NumeroOriginal = $"N-{v} / M-{v}", Descripcion = "Desc " + v, DetalleTecnico = "EJE: " + v,
                Marca = "Marca " + v, Rubro = "Rubro " + v, PrecioLista = N(neto * 2.5m), PrecioNeto = N(neto),
                PrecioPagoAnticipado = N(neto * 0.95m), EstadoStock = "HAY", UrlImagen = $"https://img/{v}.jpg",
                FechaOrigen = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc).AddDays((int)neto % 28).ToString("O"),
            };
        }

        public object?[] Parametros(int corrida) => new object?[] { corrida, Codigo, IdExterno, CodigoBarras, NumeroOriginal,
            Nombre, Descripcion, DetalleTecnico, Marca, Rubro, PrecioLista, PrecioNeto, PrecioPagoAnticipado, EstadoStock,
            Stock, AlicuotaIva, UrlImagen, FechaOrigen, Activo };
    }

    /// <summary>Lo que hace un workflow de n8n: las sentencias del contrato, con parámetros posicionales en texto.</summary>
    private sealed class N8n : IAsyncDisposable
    {
        private static readonly Lazy<Dictionary<string, string>> Sentencias = new(LeerContrato);
        private readonly LaCentralDbContext _contexto;
        private readonly NpgsqlConnection _conexion;
        private N8n(LaCentralDbContext contexto) { _contexto = contexto; _conexion = (NpgsqlConnection)contexto.Database.GetDbConnection(); }

        public static Dictionary<string, string> Contrato() => Sentencias.Value;

        public static async Task<N8n> ConectarAsync(PostgreSqlPruebas fixture)
        {
            var n8n = new N8n(fixture.CrearContexto());
            await n8n._conexion.OpenAsync();
            return n8n;
        }

        public async Task<int> AbrirAsync(int proveedorId, string ejecucion = "ejec")
            => (int)(await EscalarAsync("a", proveedorId, ejecucion) ?? throw new Xunit.Sdk.XunitException("a) no devolvió la corrida."));

        /// <summary>b) y después c) con el id que devolvió b), como el workflow.</summary>
        public async Task<int> ProductoAsync(int corrida, Producto producto)
        {
            var id = (int)(await EscalarAsync("b", producto.Parametros(corrida)) ?? throw new Xunit.Sdk.XunitException("b) no devolvió el renglón."));
            if (await EscalarAsync("c", corrida, id) is null) throw new Xunit.Sdk.XunitException("c) no registró el historial.");
            return id;
        }

        public async Task<(int, int)> CerrarAsync(int corrida, string estado, string mensaje = "")
            => await FilaAsync("d", corrida, estado, mensaje) ?? throw new Xunit.Sdk.XunitException("d) no devolvió la corrida.");

        public async Task<long> VencerAsync(int corrida) => (long)(await EscalarAsync("e", corrida))!;

        public async Task<(int, int)?> FilaAsync(string sentencia, params object?[] valores)
        {
            await using var comando = Comando(Contrato()[sentencia], valores, comoTexto: true);
            await using var lector = await comando.ExecuteReaderAsync();
            return await lector.ReadAsync() && !lector.IsDBNull(0) ? (lector.GetInt32(0), lector.GetInt32(1)) : null;
        }

        public async Task<object?> EscalarAsync(string sentencia, params object?[] valores)
        {
            await using var comando = Comando(Contrato()[sentencia], valores, comoTexto: true);
            var resultado = await comando.ExecuteScalarAsync();
            return resultado is DBNull ? null : resultado;
        }

        public async Task EjecutarAsync(string sql, params object?[] valores)
        {
            await using var comando = Comando(sql, valores, comoTexto: false);
            await comando.ExecuteNonQueryAsync();
        }

        // n8n manda los parámetros como texto.
        private NpgsqlCommand Comando(string sql, object?[] valores, bool comoTexto)
        {
            var comando = new NpgsqlCommand(sql, _conexion);
            foreach (var valor in valores)
                comando.Parameters.Add(new NpgsqlParameter
                {
                    Value = valor is null ? DBNull.Value : comoTexto ? Convert.ToString(valor, CultureInfo.InvariantCulture)! : valor,
                });
            return comando;
        }

        private static Dictionary<string, string> LeerContrato()
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                var ruta = Path.Combine(dir.FullName, "docs", "sprint07", "CONTRATO-N8N.md");
                if (!File.Exists(ruta)) continue;
                var texto = File.ReadAllText(ruta).Replace("\r\n", "\n");
                return Regex.Matches(texto, @"^### (\w)\) [^\n]*\n\n```sql\n(.*?)\n```", RegexOptions.Multiline | RegexOptions.Singleline)
                    .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            }
            throw new FileNotFoundException("No encontré docs/sprint07/CONTRATO-N8N.md.");
        }

        public async ValueTask DisposeAsync() => await _contexto.DisposeAsync();
    }
}

#:package Npgsql@10.0.3
#:package BCrypt.Net-Next@4.2.0
#:property PublishAot=false
// C3 · siembra de lacentral_demo. Instrucciones en README.md, al lado.
//
// Proveedores, artículos y compras entran POR LA API, no por SQL: así el stock, el costo,
// el historial de compras y los precios por modalidad salen de los mismos casos de uso que
// en producción. Por SQL van solo dos cosas que la API no puede hacer:
//   1. los dos usuarios de la demo (sin usuario no hay token para llamar a la API);
//   2. la fecha de registro de cada compra: la API estampa «ahora», y la demo necesita
//      compras al mismo artículo en fechas distintas.
//
// Datos inventados. Ningún CUIT: uno inventado puede coincidir con uno real.
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Npgsql;

static string Requerida(string nombre) =>
    Environment.GetEnvironmentVariable(nombre) is { Length: > 0 } v ? v
        : throw new InvalidOperationException($"Falta la variable {nombre}. Ver README.md.");

var cadena = new NpgsqlConnectionStringBuilder(Requerida("LACENTRAL_DEMO_PG"));
var api = Environment.GetEnvironmentVariable("LACENTRAL_DEMO_API") ?? "http://localhost:5181";
var clave = Requerida("LACENTRAL_DEMO_CLAVE");

// Solo lacentral_demo. La otra excepción es el ensayo: un esquema descartable demo_ensayo_*
// dentro de lacentral_test. Nunca lacentral, que tiene los datos reales de la empresa.
var esEnsayo = cadena.Database == "lacentral_test" && (cadena.SearchPath ?? "").StartsWith("demo_ensayo_");
if (cadena.Database != "lacentral_demo" && !esEnsayo)
    throw new InvalidOperationException($"La siembra solo escribe en lacentral_demo, no en «{cadena.Database}».");

await using var bd = NpgsqlDataSource.Create(cadena.ConnectionString);

async Task<long> Escalar(string sql) => Convert.ToInt64(await bd.CreateCommand(sql).ExecuteScalarAsync());

// Una base con datos no se siembra encima: duplicaría o mezclaría.
var ocupadas = await Escalar("SELECT (SELECT count(*) FROM proveedor) + (SELECT count(*) FROM articulo) + (SELECT count(*) FROM usuario)");
if (ocupadas > 0)
    throw new InvalidOperationException($"La base no está vacía ({ocupadas} filas entre proveedor, articulo y usuario). La siembra corre una sola vez, sobre una base recién creada.");

// ── 1. Usuarios, por SQL. El hash es el mismo BCrypt de ServicioHash.
foreach (var (nombre, rol, sucursal) in new[] { ("admin.demo", "ADMINISTRADOR", 1), ("operador.demo", "OPERADOR", 2) })
{
    await using var cmd = bd.CreateCommand(
        "INSERT INTO usuario (nombre_usuario, hash_contrasena, rol_id, sucursal_id) " +
        "SELECT $1, $2, r.id, $3 FROM rol r WHERE r.nombre = $4");
    cmd.Parameters.AddWithValue(nombre);
    cmd.Parameters.AddWithValue(BCrypt.Net.BCrypt.HashPassword(clave));
    cmd.Parameters.AddWithValue((short)sucursal);
    cmd.Parameters.AddWithValue(rol);
    if (await cmd.ExecuteNonQueryAsync() != 1) throw new InvalidOperationException($"No existe el rol {rol}.");
}

// ── 2. Sesión de administrador contra la API.
using var http = new HttpClient { BaseAddress = new Uri(api) };
var sesion = await Enviar("/api/acceso/login", new { nombreUsuario = "admin.demo", contrasena = clave });
http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sesion.GetProperty("token").GetString());

async Task<JsonElement> Enviar(string ruta, object cuerpo)
{
    var r = await http.PostAsJsonAsync(ruta, cuerpo);
    var texto = await r.Content.ReadAsStringAsync();
    if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"POST {ruta} → {(int)r.StatusCode}: {texto}");
    return JsonDocument.Parse(texto).RootElement;
}

// ── 3. Cinco proveedores. Prefijo de código y factor de precio propios de cada uno.
var proveedores = new (string Codigo, string RazonSocial, string Prefijo, decimal Factor)[]
{
    ("DEMO-01", "Autopartes del Centro SRL (demo)",      "AC-",  1.00m),
    ("DEMO-02", "Repuestos Mediterránea SA (demo)",      "RM",   0.94m),
    ("DEMO-03", "Casa del Rodamiento Sur SRL (demo)",    "CRS/", 1.07m),
    ("DEMO-04", "Distribuidora Mayorista Norte SA (demo)", "DMN.", 0.98m),
    ("DEMO-05", "Frenos y Embragues Córdoba SRL (demo)", "FEC",  1.03m),
};
var idProveedor = new int[proveedores.Length];
for (var p = 0; p < proveedores.Length; p++)
{
    // El alta de proveedor responde con el código, no con el Id: se lee de la base.
    await Enviar("/api/proveedores", new { codigo = proveedores[p].Codigo, razonSocial = proveedores[p].RazonSocial });
    idProveedor[p] = (int)await Escalar($"SELECT id FROM proveedor WHERE codigo = '{proveedores[p].Codigo}'");
}

// ── 4. Treinta artículos. Cada uno con código en 2 a 4 proveedores (D2: uno por proveedor).
var articulos = new (string Nombre, decimal Base)[]
{
    ("Filtro de aceite motor nafta 1.6", 8_900m),       ("Filtro de aire panel rectangular", 11_400m),
    ("Filtro de combustible diésel en línea", 14_200m), ("Filtro de habitáculo con carbón activado", 9_800m),
    ("Bujía de encendido níquel (juego x4)", 21_500m),  ("Bujía de encendido iridio", 12_300m),
    ("Pastillas de freno delanteras", 38_700m),         ("Pastillas de freno traseras", 31_200m),
    ("Disco de freno ventilado 256 mm", 54_900m),       ("Zapatas de freno traseras", 27_600m),
    ("Líquido de frenos DOT 4 500 ml", 7_400m),         ("Kit de embrague con rulemán", 189_000m),
    ("Rulemán de rueda delantera", 42_800m),            ("Rodamiento 6203-2RS", 4_300m),
    ("Rodamiento 6305-2RS", 7_900m),                    ("Correa de distribución con tensor", 96_500m),
    ("Correa poly-V 6PK1200", 18_300m),                 ("Bomba de agua con junta", 67_200m),
    ("Termostato 88 °C", 15_600m),                      ("Amortiguador delantero gas", 88_400m),
    ("Amortiguador trasero gas", 72_100m),              ("Rótula de suspensión inferior", 24_700m),
    ("Extremo de dirección", 19_900m),                  ("Bieleta de barra estabilizadora", 13_800m),
    ("Aceite 10W-40 semisintético 4 L", 36_500m),       ("Aceite 5W-30 sintético 4 L", 58_200m),
    ("Lámpara H4 12 V 60/55 W", 5_600m),                ("Escobilla limpiaparabrisas 20\"", 8_100m),
    ("Batería 12 V 65 Ah", 145_000m),                   ("Sensor de oxígeno (sonda lambda)", 79_300m),
};
// Proveedores de cada artículo: un patrón fijo que reparte 2, 3 o 4 por artículo y deja
// varios artículos comprados a todos, para que la comparación tenga con qué.
int[] ProveedoresDe(int a) => (a % 3) switch
{
    0 => new[] { a % 5, (a + 1) % 5 },
    1 => new[] { a % 5, (a + 2) % 5, (a + 3) % 5 },
    _ => new[] { a % 5, (a + 1) % 5, (a + 2) % 5, (a + 4) % 5 },
};
var idArticulo = new int[articulos.Length];
for (var a = 0; a < articulos.Length; a++)
{
    var codigos = ProveedoresDe(a).Select(p => new
    {
        proveedorId = idProveedor[p],
        codigo = $"{proveedores[p].Prefijo}{(a + 1) * 137 + p * 1009:D5}",
    });
    idArticulo[a] = (await Enviar("/api/articulos", new { nombre = articulos[a].Nombre, codigosProveedor = codigos }))
        .GetProperty("id").GetInt32();
}

// ── 5. Compras. Tres facturas por proveedor, en meses distintos; cada artículo entra en dos
// de las tres. El precio sube ~4 % por mes y cada proveedor tiene su factor: el mismo
// artículo queda comprado a precios distintos, a proveedores distintos y en fechas distintas.
var meses = new[] { new DateOnly(2026, 8, 6), new DateOnly(2026, 9, 9), new DateOnly(2026, 10, 14) };
var compras = new List<(DateOnly Fecha, int P, int Mes)>();
for (var m = 0; m < meses.Length; m++)
    for (var p = 0; p < proveedores.Length; p++)
        compras.Add((meses[m].AddDays(p * 3), p, m));

// En orden cronológico: el costo vigente del artículo queda en la última compra, como en la realidad.
foreach (var (fecha, p, m) in compras.OrderBy(c => c.Fecha))
{
    var lineas = Enumerable.Range(0, articulos.Length)
        .Where(a => ProveedoresDe(a).Contains(p) && (a + m) % 3 != 0)
        .Select(a => new
        {
            articuloId = idArticulo[a],
            sucursalId = (short)((a + m) % 2 + 1),
            cantidad = (decimal)(2 + (a + p + m) % 9),
            precioUnitario = Math.Round(articulos[a].Base * proveedores[p].Factor * (1m + 0.04m * m), 2),
            descuentoPorcentaje = (a + m) % 4 == 0 ? 5m : 0m,
        }).ToList();
    if (lineas.Count == 0) continue;
    await Enviar("/api/facturas-compra", new
    {
        proveedorId = idProveedor[p],
        numero = $"0001-{(m + 1) * 1000 + p + 1:D8}",
        fechaEmision = fecha,
        lineas,
    });
}

// ── 6. Fecha de registro = día de emisión, 10 h de Córdoba. El historial copia la de su factura.
var fechas = await bd.CreateCommand("""
    WITH f AS (
        UPDATE factura_compra
        SET    fecha_registro = (fecha_emision + time '10:00') AT TIME ZONE 'America/Argentina/Cordoba'
        RETURNING id, fecha_registro)
    UPDATE articulo_historial_compra h SET fecha = f.fecha_registro FROM f WHERE h.factura_compra_id = f.id
    """).ExecuteNonQueryAsync();

Console.WriteLine($"""
    Sembrada {(esEnsayo ? "(ENSAYO) " : "")}{cadena.Database}:
      usuarios            {await Escalar("SELECT count(*) FROM usuario")}
      proveedores         {await Escalar("SELECT count(*) FROM proveedor")}
      artículos           {await Escalar("SELECT count(*) FROM articulo")}
      códigos             {await Escalar("SELECT count(*) FROM articulo_codigo_alternativo")}
      facturas de compra  {await Escalar("SELECT count(*) FROM factura_compra")}
      líneas              {await Escalar("SELECT count(*) FROM factura_compra_detalle")}
      historial (fechas)  {fechas}
      fechas distintas    {await Escalar("SELECT count(DISTINCT fecha::date) FROM articulo_historial_compra")}
    """);

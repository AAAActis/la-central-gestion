#:package Npgsql@10.0.3
#:package BCrypt.Net-Next@4.2.0
#:property PublishAot=false
// Pone LACENTRAL_DEMO_CLAVE como contraseña de admin.demo y operador.demo. Solo lacentral_demo.
// La clave no va al repo: cada uno la define en su máquina. Ver docs/sprint05/COMO-PROBARLO.md.
using Npgsql;

static string Requerida(string nombre) =>
    Environment.GetEnvironmentVariable(nombre) is { Length: > 0 } v ? v
        : throw new InvalidOperationException($"Falta la variable {nombre}. Ver docs/sprint05/COMO-PROBARLO.md.");

var cadena = new NpgsqlConnectionStringBuilder(Requerida("LACENTRAL_DEMO_PG"));
if (cadena.Database != "lacentral_demo")
    throw new InvalidOperationException($"Solo escribe en lacentral_demo, no en «{cadena.Database}».");
var clave = Requerida("LACENTRAL_DEMO_CLAVE");
if (clave.Length < 6) throw new InvalidOperationException("La clave tiene que tener al menos 6 caracteres (HU-ACC-02).");

await using var bd = NpgsqlDataSource.Create(cadena.ConnectionString);
var n = 0;
foreach (var usuario in new[] { "admin.demo", "operador.demo" })
{
    // El mismo BCrypt que ServicioHash; un hash por usuario, con su propia sal.
    await using var cmd = bd.CreateCommand("UPDATE usuario SET hash_contrasena = $1 WHERE nombre_usuario = $2");
    cmd.Parameters.AddWithValue(BCrypt.Net.BCrypt.HashPassword(clave));
    cmd.Parameters.AddWithValue(usuario);
    n += await cmd.ExecuteNonQueryAsync();
}
if (n != 2) throw new InvalidOperationException($"Se esperaban 2 usuarios de demo y se actualizaron {n}. ¿Está sembrada la base?");
Console.WriteLine("Clave restablecida para admin.demo y operador.demo.");

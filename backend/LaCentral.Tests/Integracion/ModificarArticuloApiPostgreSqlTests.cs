using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LaCentral.Api.Seguridad;
using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// HU-ART-03 de punta a punta: pipeline HTTP real (routing, DI, [Authorize], el rol leído
/// del token por ContextoUsuario, mapeo Result → HTTP) contra PostgreSQL real, en el esquema
/// aislado del fixture. Nada mockeado.
/// </summary>
[Trait("Api", "PostgreSQL")]
public class ModificarArticuloApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ModificarArticuloApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol, int usuarioId)
    {
        var cliente = fixture.Liberar(_factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(ConfiguracionJwt));
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<LaCentralDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<LaCentralDbContext>>();
                servicios.AddDbContext<LaCentralDbContext>(o => o.UseNpgsql(fixture.ConexionAislada));
            });
        })).CreateClient();

        var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(ConfiguracionJwt).Build())
            .GenerarToken(usuarioId, "test.articulos", rol, 1);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return cliente;
    }

    private static async Task<int> CrearProveedorAsync(PostgreSqlPruebas fixture, string codigo)
    {
        await using var contexto = fixture.CrearContexto();
        var proveedor = new Proveedor { Codigo = codigo, RazonSocial = $"Proveedor {codigo}", Activo = true };
        contexto.Proveedor.Add(proveedor);
        await contexto.SaveChangesAsync();
        return proveedor.Id;
    }

    private static async Task<int> CrearArticuloAsync(HttpClient cliente, string nombre, params (int ProveedorId, string Codigo)[] codigos)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new
        {
            nombre,
            ubicacionDeposito = "Estante 1",
            codigosProveedor = codigos.Select(c => new { proveedorId = c.ProveedorId, codigo = c.Codigo }).ToArray()
        });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    private static async Task<Articulo> LeerAsync(PostgreSqlPruebas fixture, int id)
    {
        await using var relectura = fixture.CrearContexto();
        return await relectura.Articulo.AsNoTracking().Include(a => a.ArticuloCodigoAlternativo)
            .SingleAsync(a => a.Id == id);
    }

    private static async Task<string?> DetalleAsync(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();

    // CA1 + CA2 por HTTP: el operador modifica datos comunes y suma el código de otro proveedor.
    [PostgreSqlFact]
    public async Task Put_OperadorModificaDatosYAgregaCodigo_204YSeGuarda()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía", (norte, "BPR-6ES"));

        var respuesta = await cliente.PutAsJsonAsync($"/api/articulos/{id}", new
        {
            nombre = "Bujía NGK", ubicacionDeposito = "Pasillo 9",
            codigosProveedor = new[] { new { proveedorId = sur, codigo = "ngk-1" } }
        });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        var articulo = await LeerAsync(fixture, id);
        Assert.Equal(("Bujía NGK", "Pasillo 9", $"LC-{id:D6}"), (articulo.Nombre, articulo.UbicacionDeposito, articulo.CodigoInterno));
        Assert.Equal(new[] { ("BPR-6ES", norte), ("NGK-1", sur) },
            articulo.ArticuloCodigoAlternativo.OrderBy(c => c.ProveedorId).Select(c => (c.Codigo, c.ProveedorId)));
    }

    // El rol sale del token, no del cuerpo.
    [PostgreSqlFact]
    public async Task Put_OperadorCambiaElCodigoInterno_403YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía");

        var respuesta = await cliente.PutAsJsonAsync($"/api/articulos/{id}",
            new { nombre = "Bujía NGK", codigoInterno = "BUJIA-01", rol = "ADMINISTRADOR" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Contains("administrador", await DetalleAsync(respuesta));
        var articulo = await LeerAsync(fixture, id);
        Assert.Equal(("Bujía", $"LC-{id:D6}"), (articulo.Nombre, articulo.CodigoInterno));
    }

    [PostgreSqlFact]
    public async Task Put_AdministradorCambiaElCodigoInterno_204YSeGuarda()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "ADMINISTRADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía");

        var respuesta = await cliente.PutAsJsonAsync($"/api/articulos/{id}", new { nombre = "Bujía", codigoInterno = " bujia-01 " });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("BUJIA-01", (await LeerAsync(fixture, id)).CodigoInterno);
    }

    [PostgreSqlFact]
    public async Task Delete_OperadorRetiraUnCodigo_403YElCodigoQueda()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía", (norte, "BPR-6ES"));

        var respuesta = await cliente.DeleteAsync($"/api/articulos/{id}/codigos/{norte}");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Single((await LeerAsync(fixture, id)).ArticuloCodigoAlternativo);
    }

    [PostgreSqlFact]
    public async Task Delete_AdministradorRetiraUnCodigo_204YSeBorra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        using var cliente = Cliente(fixture, "ADMINISTRADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía", (norte, "BPR-6ES"));

        var respuesta = await cliente.DeleteAsync($"/api/articulos/{id}/codigos/{norte}");

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Empty((await LeerAsync(fixture, id)).ArticuloCodigoAlternativo);
    }

    // CA3 por HTTP: 409 informando de qué artículo es el código.
    [PostgreSqlFact]
    public async Task Put_CodigoDeOtroArticulo_409InformandoCual()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var duenio = await CrearArticuloAsync(cliente, "Filtro de aire", (norte, "FA-100"));
        var id = await CrearArticuloAsync(cliente, "Bujía");

        var respuesta = await cliente.PutAsJsonAsync($"/api/articulos/{id}", new
        {
            nombre = "Bujía", codigosProveedor = new[] { new { proveedorId = sur, codigo = "FA-100" } }
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains($"LC-{duenio:D6}", await DetalleAsync(respuesta));
        Assert.Empty((await LeerAsync(fixture, id)).ArticuloCodigoAlternativo);
    }

    [PostgreSqlFact]
    public async Task Put_Inexistente_404()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "OPERADOR", usuario);

        var respuesta = await cliente.PutAsJsonAsync("/api/articulos/999999", new { nombre = "Bujía" });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // CA5 por HTTP: baja → modificar da 400 con "reactivalo" → reactivación → modificar 204.
    // La ficha muestra el motivo mientras está dado de baja.
    [PostgreSqlFact]
    public async Task Baja_Modificar_Reactivar_Modificar()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía");

        Assert.Equal(HttpStatusCode.NoContent,
            (await cliente.PostAsJsonAsync($"/api/articulos/{id}/baja", new { motivo = "Discontinuado" })).StatusCode);
        var ficha = await cliente.GetFromJsonAsync<JsonElement>($"/api/articulos/{id}");
        Assert.False(ficha.GetProperty("activo").GetBoolean());
        Assert.Equal("Discontinuado", ficha.GetProperty("motivoBaja").GetString());

        var denegada = await cliente.PutAsJsonAsync($"/api/articulos/{id}", new { nombre = "Bujía corregida" });
        Assert.Equal(HttpStatusCode.BadRequest, denegada.StatusCode);
        Assert.Contains("reactiv", await DetalleAsync(denegada), StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsync($"/api/articulos/{id}/reactivacion", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await cliente.PutAsJsonAsync($"/api/articulos/{id}", new { nombre = "Bujía corregida" })).StatusCode);

        var articulo = await LeerAsync(fixture, id);
        Assert.Equal(("Bujía corregida", true, "Discontinuado"), (articulo.Nombre, articulo.Activo, articulo.MotivoBaja));
        await using var relectura = fixture.CrearContexto();
        var historial = await relectura.ArticuloHistorialBaja.AsNoTracking().SingleAsync(h => h.ArticuloId == id);
        Assert.Equal((usuario, (int?)usuario), (historial.UsuarioBajaId!.Value, historial.UsuarioReactivacionId));
    }

    // La ficha trae la ubicación, para editarla.
    [PostgreSqlFact]
    public async Task Get_Ficha_TraeLaUbicacion()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "OPERADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía");

        var ficha = await cliente.GetFromJsonAsync<JsonElement>($"/api/articulos/{id}");

        Assert.Equal("Estante 1", ficha.GetProperty("ubicacionDeposito").GetString());
        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("motivoBaja").ValueKind);
    }

    // El contrato del 400 por formato: ValidationProblemDetails con el error POR CAMPO.
    [PostgreSqlFact]
    public async Task Put_FormatoInvalido_400ConErroresPorCampoYNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        using var cliente = Cliente(fixture, "ADMINISTRADOR", usuario);
        var id = await CrearArticuloAsync(cliente, "Bujía");

        var respuesta = await cliente.PutAsJsonAsync($"/api/articulos/{id}", new
        {
            ubicacionDeposito = new string('u', 61),
            codigoInterno = new string('c', 41)
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.True((await respuesta.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out var errores),
            "El 400 tiene que traer los errores por campo.");
        Assert.Equal(new[] { "CodigoInterno", "Nombre", "UbicacionDeposito" },
            errores.EnumerateObject().Select(e => e.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal("Bujía", (await LeerAsync(fixture, id)).Nombre);
    }
}

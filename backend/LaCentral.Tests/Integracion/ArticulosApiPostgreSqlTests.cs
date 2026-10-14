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
/// HU-ART-01 de punta a punta: pipeline HTTP real (routing, DI, [Authorize], mapeo
/// Result → HTTP) contra PostgreSQL real, en el esquema aislado del fixture. Nada mockeado.
/// </summary>
[Trait("Api", "PostgreSQL")]
public class ArticulosApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public ArticulosApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private HttpClient Cliente(PostgreSqlPruebas fixture, string rol)
    {
        var cliente = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(ConfiguracionJwt));
            builder.ConfigureTestServices(servicios =>
            {
                servicios.RemoveAll<DbContextOptions<LaCentralDbContext>>();
                servicios.RemoveAll<IDbContextOptionsConfiguration<LaCentralDbContext>>();
                servicios.AddDbContext<LaCentralDbContext>(o => o.UseNpgsql(fixture.ConexionAislada));
            });
        }).CreateClient();

        var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(ConfiguracionJwt).Build())
            .GenerarToken(1, "test.articulos", rol, 1);
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

    [PostgreSqlFact]
    public async Task Post_Alta_201ConIdYLocation()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedor = await CrearProveedorAsync(fixture, "PRV-API");
        using var cliente = Cliente(fixture, "OPERADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new
        {
            nombre = "Filtro de aceite",
            ubicacionDeposito = "Pasillo 2",
            codigosProveedor = new[] { new { proveedorId = proveedor, codigo = " fa-100 " } }
        });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var id = cuerpo.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal($"LC-{id:D6}", cuerpo.GetProperty("codigoInterno").GetString());
        Assert.Equal($"/api/articulos/{id}", respuesta.Headers.Location?.OriginalString);

        await using var relectura = fixture.CrearContexto();
        var articulo = await relectura.Articulo.AsNoTracking().Include(a => a.ArticuloCodigoAlternativo)
            .SingleAsync(a => a.Id == id);
        Assert.Equal("Filtro de aceite", articulo.Nombre);
        Assert.Equal("FA-100", Assert.Single(articulo.ArticuloCodigoAlternativo).Codigo);
    }

    [PostgreSqlFact]
    public async Task Post_CodigoDeOtroArticulo_409InformandoCual()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedor = await CrearProveedorAsync(fixture, "PRV-API");
        await using (var setup = fixture.CrearContexto())
        {
            setup.Articulo.Add(new Articulo { CodigoInterno = "BYC1352", Nombre = "Correa de distribución", Activo = true });
            await setup.SaveChangesAsync();
        }
        using var cliente = Cliente(fixture, "ADMINISTRADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new
        {
            nombre = "Otro artículo",
            codigosProveedor = new[] { new { proveedorId = proveedor, codigo = "byc1352" } }
        });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        var detalle = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();
        Assert.Contains("BYC1352", detalle);
        Assert.Contains("Correa de distribución", detalle);
    }

    [PostgreSqlFact]
    public async Task Post_RolSinPermiso_403YNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "INVITADO");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new { nombre = "Sin permiso" });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.Articulo.AnyAsync(a => a.Nombre == "Sin permiso"));
    }
}

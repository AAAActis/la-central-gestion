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
        // El código de la respuesta tiene que ser el persistido, no un TMP- que quedó.
        Assert.Equal($"LC-{id:D6}", articulo.CodigoInterno);
        Assert.Equal("FA-100", Assert.Single(articulo.ArticuloCodigoAlternativo).Codigo);
    }

    // Un artículo sin códigos de proveedor es válido: el cuerpo puede omitir la lista.
    [PostgreSqlFact]
    public async Task Post_SinCodigosProveedor_201YSeGuardaSinCodigos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new { nombre = "Artículo sin códigos" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await using var relectura = fixture.CrearContexto();
        var articulo = await relectura.Articulo.AsNoTracking().Include(a => a.ArticuloCodigoAlternativo)
            .SingleAsync(a => a.Id == id);
        Assert.Equal("Artículo sin códigos", articulo.Nombre);
        Assert.Empty(articulo.ArticuloCodigoAlternativo);
    }

    // El contrato del 400 por formato: ValidationProblemDetails con el error POR CAMPO,
    // que el caso de uso (un ProblemDetails con un solo detalle) no da.
    [PostgreSqlFact]
    public async Task Post_FormatoInvalido_400ConErroresPorCampoYNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new
        {
            ubicacionDeposito = new string('u', 61),
            codigosProveedor = new object[]
            {
                new { proveedorId = 1, codigo = new string('c', 61) },
                new { proveedorId = 1 }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        var campos = errores.EnumerateObject().Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Nombre", campos);
        Assert.Contains("UbicacionDeposito", campos);
        Assert.Contains("CodigosProveedor[0].Codigo", campos);
        Assert.Contains("CodigosProveedor[1].Codigo", campos);

        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.Articulo.AnyAsync());
    }

    [PostgreSqlFact]
    public async Task Post_NombreDeMasDe200_400ConErrorDeNombre()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new { nombre = new string('n', 201) });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var errores = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("errors");
        Assert.Contains("Nombre", errores.EnumerateObject().Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
    }

    // El 22021 (NUL en un texto) se traduce en el manejador global, no por endpoint.
    [PostgreSqlFact]
    public async Task Post_NombreConCaracterNul_400YNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");

        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", new { nombre = "Filtro\u0000de aceite" });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("caracteres no válidos", problema.GetProperty("detail").GetString());
        Assert.False(problema.TryGetProperty("errors", out _));
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.Articulo.AnyAsync());
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

    // ---- HU-ART-02 · GET /api/articulos y GET /api/articulos/{id} ----

    private static async Task<int> AltaAsync(HttpClient cliente, object cuerpo)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/articulos", cuerpo);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        return (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    [PostgreSqlFact]
    public async Task Get_Busqueda_200ConIdCodigoInternoNombreYCodigosDeProveedor()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var proveedor = await CrearProveedorAsync(fixture, "PRV-API");
        using var cliente = Cliente(fixture, "OPERADOR");
        var id = await AltaAsync(cliente, new
        {
            nombre = "Filtro de aceite",
            codigosProveedor = new[] { new { proveedorId = proveedor, codigo = "fa-100" } }
        });

        var respuesta = await cliente.GetAsync("/api/articulos?texto=filtro");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pagina = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, pagina.GetProperty("total").GetInt32());
        Assert.Equal(1, pagina.GetProperty("pagina").GetInt32());
        Assert.Equal(20, pagina.GetProperty("tamanoPagina").GetInt32());
        var articulo = Assert.Single(pagina.GetProperty("articulos").EnumerateArray());
        Assert.Equal(id, articulo.GetProperty("id").GetInt32());
        Assert.Equal($"LC-{id:D6}", articulo.GetProperty("codigoInterno").GetString());
        Assert.Equal("Filtro de aceite", articulo.GetProperty("nombre").GetString());
        var codigo = Assert.Single(articulo.GetProperty("codigosProveedor").EnumerateArray());
        Assert.Equal(proveedor, codigo.GetProperty("proveedorId").GetInt32());
        Assert.Equal("Proveedor PRV-API", codigo.GetProperty("razonSocial").GetString());
        Assert.Equal("FA-100", codigo.GetProperty("codigo").GetString());
    }

    [PostgreSqlFact]
    public async Task Get_TextoVacioOAusente_400()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");

        foreach (var url in new[] { "/api/articulos?texto=", "/api/articulos?texto=%20%20", "/api/articulos" })
            Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync(url)).StatusCode);
    }

    [PostgreSqlFact]
    public async Task Get_SinResultados_200ConListaVaciaNunca404()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "ADMINISTRADOR");

        var respuesta = await cliente.GetAsync("/api/articulos?texto=inexistente");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var pagina = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, pagina.GetProperty("articulos").GetArrayLength());
        Assert.Equal(0, pagina.GetProperty("total").GetInt32());
    }

    [PostgreSqlFact]
    public async Task Get_PaginacionYFiltroDeActivosExplicitos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var setup = fixture.CrearContexto())
        {
            for (var i = 1; i <= 3; i++)
                setup.Articulo.Add(new Articulo { CodigoInterno = $"BUJ-{i}", Nombre = "Bujía", Activo = true });
            setup.Articulo.Add(new Articulo
            {
                CodigoInterno = "BUJ-4", Nombre = "Bujía", Activo = false,
                MotivoBaja = "Discontinuada", FechaBaja = DateTime.UtcNow
            });
            await setup.SaveChangesAsync();
        }
        using var cliente = Cliente(fixture, "OPERADOR");

        var segunda = await cliente.GetAsync("/api/articulos?texto=bujia&pagina=2&tamanoPagina=2");
        var conInactivos = await cliente.GetAsync("/api/articulos?texto=bujia&incluirInactivos=true");

        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(HttpStatusCode.OK, conInactivos.StatusCode);
        var paginaDos = await segunda.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, paginaDos.GetProperty("total").GetInt32());
        Assert.Equal(2, paginaDos.GetProperty("pagina").GetInt32());
        Assert.Equal(2, paginaDos.GetProperty("tamanoPagina").GetInt32());
        Assert.Equal(1, paginaDos.GetProperty("articulos").GetArrayLength());
        Assert.Equal(4, (await conInactivos.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.GetAsync("/api/articulos?texto=bujia&tamanoPagina=101")).StatusCode);
    }

    // Modalidad, Base y RazonAusencia viajan por NOMBRE, no como número.
    [PostgreSqlFact]
    public async Task Get_Ficha_200ConLasSeisModalidadesPorNombreYSuRazon()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "ADMINISTRADOR");
        var id = await AltaAsync(cliente, new { nombre = "Correa" });

        var respuesta = await cliente.GetAsync($"/api/articulos/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var ficha = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, ficha.GetProperty("id").GetInt32());
        Assert.Equal($"LC-{id:D6}", ficha.GetProperty("codigoInterno").GetString());
        Assert.Equal(JsonValueKind.Null, ficha.GetProperty("costoVigente").ValueKind);
        var precios = ficha.GetProperty("precios").EnumerateArray()
            .Select(p => (
                Modalidad: p.GetProperty("modalidad").ToString(),
                Base: p.GetProperty("base").ToString(),
                Precio: p.GetProperty("precio").ValueKind,
                Razon: p.GetProperty("razonAusencia").ToString()))
            .ToArray();
        Assert.Equal(new (string, string, JsonValueKind, string)[]
        {
            ("Lista", "CostoArticulo", JsonValueKind.Null, "SinCosto"),
            ("Tarjeta3", "", JsonValueKind.Null, "TasaSinDefinir"),
            ("Tarjeta6", "", JsonValueKind.Null, "TasaSinDefinir"),
            ("EfectivoTransferencia", "PrecioLista", JsonValueKind.Null, "SinCosto"),
            ("Costo", "CostoArticulo", JsonValueKind.Null, "SinCosto"),
            ("CasoEspecial", "", JsonValueKind.Null, "CasoEspecialNoFijado"),
        }, precios);
    }

    [PostgreSqlFact]
    public async Task Get_FichaInexistente_404()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var cliente = Cliente(fixture, "OPERADOR");
        var id = await AltaAsync(cliente, new { nombre = "Existe" });

        var respuesta = await cliente.GetAsync($"/api/articulos/{checked(id + 1)}");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("no existe", cuerpo);
    }

    [PostgreSqlFact]
    public async Task Get_RolSinPermiso_403YSinToken_401()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        using var invitado = Cliente(fixture, "INVITADO");
        using var anonimo = Cliente(fixture, "OPERADOR");
        anonimo.DefaultRequestHeaders.Authorization = null;

        foreach (var url in new[] { "/api/articulos?texto=filtro", "/api/articulos/1" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await invitado.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(url)).StatusCode);
        }
    }
}

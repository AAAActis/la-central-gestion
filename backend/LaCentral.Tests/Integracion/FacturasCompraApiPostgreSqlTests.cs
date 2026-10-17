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
/// POST /api/facturas-compra de punta a punta: JWT real, controlador, caso de uso,
/// repositorio y PostgreSQL en un esquema aislado. El usuario sale del token.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class FacturasCompraApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string JwtKey = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuer = "LaCentralTests";
    private const string JwtAudience = "LaCentralTestsAudience";

    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = JwtKey, ["Jwt:Issuer"] = JwtIssuer, ["Jwt:Audience"] = JwtAudience
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public FacturasCompraApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
        => (_factory, _salida) = (factory, salida);

    private HttpClient Cliente(PostgreSqlPruebas fixture, string? rol, int usuarioId = 1)
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

        if (rol is not null)
        {
            var token = new GeneradorToken(new ConfigurationBuilder().AddInMemoryCollection(ConfiguracionJwt).Build())
                .GenerarToken(usuarioId, "test.compras", rol, 1);
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return cliente;
    }

    private sealed record Sembrado(int UsuarioId, int ProveedorId, int ArticuloId, short SucursalId);

    private static async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuarioId = await fixture.CrearUsuarioAsync("test.compras");
        await using var contexto = fixture.CrearContexto();
        var sucursal = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
        var proveedor = new Proveedor { Codigo = "PRV-API-FCO", RazonSocial = "Proveedor API", Activo = true };
        contexto.Proveedor.Add(proveedor);
        var articulo = new Articulo
        {
            CodigoInterno = "API-FCO-1", Nombre = "Artículo API", Activo = true,
            Stock = { new Stock { SucursalId = sucursal, Cantidad = 1m } }
        };
        contexto.Articulo.Add(articulo);
        await contexto.SaveChangesAsync();
        return new(usuarioId, proveedor.Id, articulo.Id, sucursal);
    }

    private static object Cuerpo(Sembrado s, string numero = "B-0002-00000045", bool conLineas = true) => new
    {
        proveedorId = s.ProveedorId,
        numero,
        fechaEmision = "2026-10-01",
        fechaVencimiento = "2026-10-31",
        percepcionIva = 21.5m,
        percepcionIibb = 3m,
        percepcionMunicipal = 0.5m,
        percepcionGanancias = 0m,
        lineas = conLineas
            ? new[] { new { articuloId = s.ArticuloId, sucursalId = s.SucursalId, cantidad = 4m, precioUnitario = 50m, descuentoPorcentaje = 20m } }
            : Array.Empty<object>()
    };

    [PostgreSqlFact]
    public async Task Post_Valida_201ConIdYLocation_YLaCompraQuedaEscritaConElUsuarioDelToken()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal($"/api/facturas-compra/{id}", respuesta.Headers.Location?.OriginalString);

        await using var relectura = fixture.CrearContexto();
        var factura = await relectura.FacturaCompra.AsNoTracking().SingleAsync(f => f.Id == id);
        Assert.Equal((s.ProveedorId, "B-0002-00000045", s.UsuarioId), (factura.ProveedorId, factura.Numero, factura.UsuarioId));
        Assert.Equal((new DateOnly(2026, 10, 1), (DateOnly?)new DateOnly(2026, 10, 31)), (factura.FechaEmision, factura.FechaVencimiento));
        Assert.Equal((21.5m, 3m, 0.5m, 0m),
            (factura.PercepcionIva, factura.PercepcionIibb, factura.PercepcionMunicipal, factura.PercepcionGanancias));
        Assert.Equal(5m, await relectura.Stock.Where(x => x.ArticuloId == s.ArticuloId && x.SucursalId == s.SucursalId)
            .Select(x => x.Cantidad).SingleAsync());
        Assert.Equal(40m, await relectura.Articulo.Where(a => a.Id == s.ArticuloId).Select(a => a.PrecioCosto).SingleAsync());
    }

    [PostgreSqlFact]
    public async Task Post_SinLineas_400YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "ADMINISTRADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s, conLineas: false));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.FacturaCompra.AnyAsync());
    }

    [PostgreSqlFact]
    public async Task Post_SinFechaDeEmision_400YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var cuerpo = JsonSerializer.SerializeToNode(Cuerpo(s))!.AsObject();
        cuerpo.Remove("fechaEmision");

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.FacturaCompra.AnyAsync());
    }

    // La base guarda 3 decimales de cantidad: 0,0004 se redondearía a 0 y violaría
    // fcd_cantidad_positiva (23514), que el manejador global informa como 500.
    [PostgreSqlFact]
    public Task Post_CantidadQueLaColumnaNoGuarda_400YNoEscribe() =>
        AfirmarRechazoSinEscrituraAsync(s => ("cantidad", 0.0004m));

    // Sin la validación del núcleo, la FK de sucursal (23503) sale como 500.
    [PostgreSqlFact]
    public Task Post_SucursalInexistente_400YNoEscribe() =>
        AfirmarRechazoSinEscrituraAsync(s => ("sucursalId", (decimal)short.MaxValue));

    private async Task AfirmarRechazoSinEscrituraAsync(Func<Sembrado, (string Campo, decimal Valor)> cambio)
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var cuerpo = JsonSerializer.SerializeToNode(Cuerpo(s))!.AsObject();
        var (campo, valor) = cambio(s);
        cuerpo["lineas"]![0]![campo] = valor;

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", cuerpo);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.FacturaCompra.AnyAsync());
        Assert.Equal(1m, await relectura.Stock.Where(x => x.ArticuloId == s.ArticuloId).SumAsync(x => x.Cantidad));
    }

    // HU-FCO-01 CA5 y HU-ART-05 CA-001 en el mismo recorrido: un artículo dado de alta por
    // la API (nace con las seis filas, sin costo) se compra a 100 y la ficha muestra los
    // precios recalculados. Los ejemplos del plan, de punta a punta.
    [PostgreSqlFact]
    public async Task Post_Compra_LaFichaDelArticuloMuestraLosPreciosRecalculados()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var alta = await cliente.PostAsJsonAsync("/api/articulos", new { nombre = "Correa de distribución" });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var articuloId = (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var cuerpo = JsonSerializer.SerializeToNode(Cuerpo(s))!.AsObject();
        cuerpo["lineas"] = JsonSerializer.SerializeToNode(new[]
        {
            new { articuloId, sucursalId = s.SucursalId, cantidad = 1m, precioUnitario = 100m, descuentoPorcentaje = 0m }
        });

        var compra = await cliente.PostAsJsonAsync("/api/facturas-compra", cuerpo);
        var ficha = await cliente.GetAsync($"/api/articulos/{articuloId}");

        Assert.Equal(HttpStatusCode.Created, compra.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ficha.StatusCode);
        var detalle = await ficha.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100m, detalle.GetProperty("costoVigente").GetDecimal());
        var precios = detalle.GetProperty("precios").EnumerateArray().ToDictionary(
            p => p.GetProperty("modalidad").GetString()!,
            p => (Precio: p.GetProperty("precio").ValueKind == JsonValueKind.Null ? (decimal?)null : p.GetProperty("precio").GetDecimal(),
                  Razon: p.GetProperty("razonAusencia").ToString()));
        Assert.Equal(((decimal?)150m, ""), precios["Lista"]);
        Assert.Equal(((decimal?)100m, ""), precios["Costo"]);
        Assert.Equal(((decimal?)135m, ""), precios["EfectivoTransferencia"]);
        Assert.Equal(((decimal?)null, "TasaSinDefinir"), precios["Tarjeta3"]);
        Assert.Equal(((decimal?)null, "TasaSinDefinir"), precios["Tarjeta6"]);
        Assert.Equal(((decimal?)null, "CasoEspecialNoFijado"), precios["CasoEspecial"]);
    }

    // PostgreSQL no guarda el carácter NUL en un texto (22021): es un dato inválido, no un error del servidor.
    [PostgreSqlFact]
    public async Task Post_NumeroConCaracterNul_400YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s, numero: "A\u0000B"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("caracteres no válidos", problema.GetProperty("detail").GetString());
        Assert.False(problema.TryGetProperty("errors", out _));
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.FacturaCompra.AnyAsync());
        Assert.Equal(1m, await relectura.Stock.Where(x => x.ArticuloId == s.ArticuloId).SumAsync(x => x.Cantidad));
    }

    [PostgreSqlFact]
    public async Task Post_NumeroActivoRepetido_409QueInformaLaFacturaEnConflicto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var primera = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s));
        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        var idPrimera = (await primera.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains($"Id {idPrimera}", await respuesta.Content.ReadAsStringAsync());
        await using var relectura = fixture.CrearContexto();
        Assert.Equal(1, await relectura.FacturaCompra.CountAsync());
    }

    [PostgreSqlFact]
    public async Task Post_SinToken_401()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, rol: null);

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s));

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [PostgreSqlFact]
    public async Task Post_RolSinPermiso_403()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "INVITADO", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas-compra", Cuerpo(s));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
}

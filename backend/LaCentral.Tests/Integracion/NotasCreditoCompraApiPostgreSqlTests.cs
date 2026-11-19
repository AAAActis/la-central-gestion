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
/// HU-FCO-07 (057) por HTTP, de punta a punta: JWT real, controlador, casos de uso, repositorio y
/// PostgreSQL en un esquema aislado (plantilla con el script 11). El usuario sale del token.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class NotasCreditoCompraApiPostgreSqlTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly Dictionary<string, string?> ConfiguracionJwt = new()
    {
        ["Jwt:Key"] = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!", ["Jwt:Issuer"] = "LaCentralTests",
        ["Jwt:Audience"] = "LaCentralTestsAudience"
    };

    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _salida;

    public NotasCreditoCompraApiPostgreSqlTests(WebApplicationFactory<Program> factory, ITestOutputHelper salida)
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
                .GenerarToken(usuarioId, "test.notas", rol, 1);
            cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
        return cliente;
    }

    /// <summary>Una factura de 4 unidades del artículo en la sucursal, cargada por la API.</summary>
    private sealed record Sembrado(int UsuarioId, int ProveedorId, int ArticuloId, short SucursalId, int FacturaId);

    private async Task<Sembrado> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var usuarioId = await fixture.CrearUsuarioAsync("test.notas");
        short sucursal;
        int proveedorId, articuloId;
        await using (var contexto = fixture.CrearContexto())
        {
            sucursal = await contexto.Sucursal.Where(s => !s.EsReclamos).OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
            var proveedor = new Proveedor { Codigo = "PRV-API-NC", RazonSocial = "Proveedor API", Activo = true };
            var articulo = new Articulo { CodigoInterno = "API-NC-1", Nombre = "Artículo API", Activo = true };
            contexto.AddRange(proveedor, articulo);
            contexto.Usuario.Add(new Usuario
            {
                NombreUsuario = "jefa.notas", HashContrasena = new ServicioHash().HashearClave("clave-jefa"),
                RolId = await contexto.Rol.Where(r => r.Nombre == "ADMINISTRADOR").Select(r => r.Id).SingleAsync(),
                SucursalId = sucursal, Activo = true
            });
            await contexto.SaveChangesAsync();
            (proveedorId, articuloId) = (proveedor.Id, articulo.Id);
        }
        using var cliente = Cliente(fixture, "OPERADOR", usuarioId);
        var alta = await cliente.PostAsJsonAsync("/api/facturas-compra", new
        {
            proveedorId, numero = "A-0001-00000001", fechaEmision = "2026-10-01", tipoComprobante = "A",
            lineas = new[] { new { articuloId, sucursalId = sucursal, cantidad = 4m, precioUnitario = 50m, descuentoPorcentaje = 0m } }
        });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var factura = (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return new(usuarioId, proveedorId, articuloId, sucursal, factura);
    }

    private static object Devolucion(Sembrado s, string? fechaEmision = "2026-11-05") => new
    {
        proveedorId = s.ProveedorId, numero = "0010-00021495", fechaEmision, clase = "DEVOLUCION", importeNeto = 100m,
        percepcionIva = 1.5m, percepcionIibb = (decimal?)null, percepcionMunicipal = (decimal?)null, percepcionGanancias = 2m,
        facturaIds = new[] { s.FacturaId },
        lineas = new[] { new { articuloId = s.ArticuloId, sucursalId = s.SucursalId, cantidad = 1.5m } }
    };

    private static async Task<decimal> StockAsync(PostgreSqlPruebas fixture, Sembrado s)
    {
        await using var c = fixture.CrearContexto();
        return await c.Stock.Where(x => x.ArticuloId == s.ArticuloId && x.SucursalId == s.SucursalId).Select(x => x.Cantidad).SingleAsync();
    }

    [PostgreSqlFact]
    public async Task Post_Devolucion_201ConIdYLocation_YQuedaEscritaConElUsuarioDelToken()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        Assert.Equal($"/api/notas-credito-compra/{id}", respuesta.Headers.Location?.OriginalString);
        await using var relectura = fixture.CrearContexto();
        var nota = await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == id);
        Assert.Equal((s.ProveedorId, "0010-00021495", new DateOnly(2026, 11, 5), "DEVOLUCION", 100m, (decimal?)1.5m, (decimal?)2m, s.UsuarioId, "MANUAL"),
            (nota.ProveedorId, nota.Numero, nota.FechaEmision, nota.Clase, nota.ImporteNeto, nota.PercepcionIva, nota.PercepcionGanancias, nota.UsuarioId, nota.Origen));
        Assert.Equal(2.5m, await StockAsync(fixture, s));
    }

    // CA-007: neto, 21 % de IVA, percepciones y total, sin guardar nada.
    [PostgreSqlFact]
    public async Task PostCalculo_200ConLosTotales_YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/notas-credito-compra/calculo", Devolucion(s));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var totales = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((100m, 21m, 3.5m, 124.5m), (totales.GetProperty("neto").GetDecimal(), totales.GetProperty("iva").GetDecimal(),
            totales.GetProperty("percepciones").GetDecimal(), totales.GetProperty("total").GetDecimal()));
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.NotaCreditoCompra.AnyAsync());
    }

    // CA-012: la anulación por HTTP, con la doble certificación; el stock vuelve al origen.
    [PostgreSqlFact]
    public async Task PostAnulacion_204_YLaNotaQuedaAnuladaConElUsuarioDelTokenYElQueAutorizo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var alta = await cliente.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s));
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var id = (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var respuesta = await cliente.PostAsJsonAsync($"/api/notas-credito-compra/{id}/anulacion",
            new { motivo = "Mal cargada", usuarioAutoriza = "jefa.notas", contrasenaAutoriza = "clave-jefa" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        await using var relectura = fixture.CrearContexto();
        var nota = await relectura.NotaCreditoCompra.AsNoTracking().SingleAsync(n => n.Id == id);
        var jefa = await relectura.Usuario.Where(u => u.NombreUsuario == "jefa.notas").Select(u => u.Id).SingleAsync();
        Assert.Equal(("ANULADA", "Mal cargada", (int?)s.UsuarioId, (int?)jefa),
            (nota.Estado, nota.MotivoAnulacion, nota.UsuarioAnulacionId, nota.UsuarioAutorizacionId));
        Assert.Equal(4m, await StockAsync(fixture, s));
    }

    // CA-011 por HTTP: 409 y el mensaje nombra la nota.
    [PostgreSqlFact]
    public async Task PostAnulacionDeLaFactura_ConNotaVigente_409QueLaNombra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        Assert.Equal(HttpStatusCode.Created, (await cliente.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s))).StatusCode);

        var respuesta = await cliente.PostAsJsonAsync($"/api/facturas-compra/{s.FacturaId}/anulacion",
            new { motivo = "Cargada dos veces", usuarioAutoriza = "jefa.notas", contrasenaAutoriza = "clave-jefa" });

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("0010-00021495", await respuesta.Content.ReadAsStringAsync());
    }

    [PostgreSqlFact]
    public async Task Post_SinFechaDeEmision_400YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);

        var respuesta = await cliente.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s, fechaEmision: null));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("fecha de emisión", await respuesta.Content.ReadAsStringAsync());
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.NotaCreditoCompra.AnyAsync());
    }

    // CA-009: sin sesión 401, con un rol que no es ADMINISTRADOR ni OPERADOR 403; no se escribe nada.
    [PostgreSqlFact]
    public async Task SinTokenEs401_YConRolSinPermisoEs403_YNoEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var sinToken = Cliente(fixture, rol: null);
        using var invitado = Cliente(fixture, "INVITADO", s.UsuarioId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await sinToken.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinToken.PostAsJsonAsync("/api/notas-credito-compra/calculo", Devolucion(s))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await invitado.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await invitado.PostAsJsonAsync("/api/notas-credito-compra/1/anulacion",
            new { motivo = "x", usuarioAutoriza = "jefa.notas", contrasenaAutoriza = "clave-jefa" })).StatusCode);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.NotaCreditoCompra.AnyAsync());
    }
    // CA-010 · el PDF de Expoyer (sintético, datos inventados) precarga la carga manual; nada se guarda.

    [PostgreSqlFact]
    public async Task PostPdf_NotaDeExpoyer_200ConLaPropuesta_YNoEscribeNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        await using (var c = fixture.CrearContexto())
        {
            (await c.Proveedor.FindAsync(s.ProveedorId))!.Cuit = FacturasCompra.PdfSintetico.CuitProveedor;
            (await c.FacturaCompra.FindAsync(s.FacturaId))!.Numero = "0011-00001111";
            await c.SaveChangesAsync();
        }
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        using var formulario = new MultipartFormDataContent
        {
            { new ByteArrayContent(FacturasCompra.PdfSintetico.NotaCreditoExpoyer()), "archivo", "nota.pdf" }
        };

        var respuesta = await cliente.PostAsync("/api/notas-credito-compra/pdf", formulario);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(s.ProveedorId, cuerpo.GetProperty("proveedorId").GetInt32());
        Assert.Equal("0010-00004321", cuerpo.GetProperty("numero").GetString());
        Assert.Equal("2027-02-04", cuerpo.GetProperty("fechaEmision").GetString());
        Assert.Equal("BONIFICACION", cuerpo.GetProperty("clase").GetString());
        Assert.Equal(1000m, cuerpo.GetProperty("importeNeto").GetDecimal());
        Assert.Equal([s.FacturaId], cuerpo.GetProperty("facturaIds").EnumerateArray().Select(x => x.GetInt32()));
        Assert.Equal(["0011-00002222"], cuerpo.GetProperty("facturasSinEncontrar").EnumerateArray().Select(x => x.GetString()));
        Assert.Equal(["facturaIds"], cuerpo.GetProperty("paraRevisar").EnumerateArray().Select(x => x.GetString()));
        await using var nuevo = fixture.CrearContexto();
        Assert.Equal(0, await nuevo.NotaCreditoCompra.CountAsync());
    }

    [PostgreSqlFact]
    public async Task PostPdf_UnArchivoQueNoEsPdf_400ConElPedidoDeCargaManual()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        using var formulario = new MultipartFormDataContent
        {
            { new ByteArrayContent("no soy un PDF"u8.ToArray()), "archivo", "nota.pdf" }
        };

        var respuesta = await cliente.PostAsync("/api/notas-credito-compra/pdf", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("nota de crédito por la vía manual", await respuesta.Content.ReadAsStringAsync());
    }

    [PostgreSqlFact]
    public async Task Post_DesdePdf_201_YLaFilaQuedaConOrigenPdf()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var json = JsonSerializer.SerializeToNode(Devolucion(s))!.AsObject();
        json["desdePdf"] = true;

        var respuesta = await cliente.PostAsJsonAsync("/api/notas-credito-compra", json);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var id = (await respuesta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        await using var relectura = fixture.CrearContexto();
        Assert.Equal("PDF", await relectura.NotaCreditoCompra.Where(n => n.Id == id).Select(n => n.Origen).SingleAsync());
    }

    // CA-012 · el listado por HTTP: proveedor y rango de la query, la anulada marcada.
    [PostgreSqlFact]
    public async Task Get_PorProveedorYRango_TraeLaNota_YDespuesDeAnularlaVieneMarcada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        var alta = await cliente.PostAsJsonAsync("/api/notas-credito-compra", Devolucion(s));
        var id = (await alta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var url = $"/api/notas-credito-compra?proveedorId={s.ProveedorId}&emisionDesde=2026-11-05&emisionHasta=2026-11-05";

        var antes = await cliente.GetFromJsonAsync<JsonElement>(url);
        Assert.Equal(HttpStatusCode.NoContent, (await cliente.PostAsJsonAsync($"/api/notas-credito-compra/{id}/anulacion",
            new { motivo = "Mal cargada", usuarioAutoriza = "jefa.notas", contrasenaAutoriza = "clave-jefa" })).StatusCode);
        var despues = await cliente.GetFromJsonAsync<JsonElement>(url);

        var fila = Assert.Single(antes.GetProperty("items").EnumerateArray());
        Assert.Equal((id, "0010-00021495", "Proveedor API", "DEVOLUCION", 100m, false),
            (fila.GetProperty("id").GetInt32(), fila.GetProperty("numero").GetString(), fila.GetProperty("proveedor").GetString(),
             fila.GetProperty("clase").GetString(), fila.GetProperty("importeNeto").GetDecimal(), fila.GetProperty("anulada").GetBoolean()));
        Assert.True(Assert.Single(despues.GetProperty("items").EnumerateArray()).GetProperty("anulada").GetBoolean());
        var fuera = await cliente.GetFromJsonAsync<JsonElement>($"/api/notas-credito-compra?emisionDesde=2026-11-06");
        Assert.Equal(0, fuera.GetProperty("total").GetInt32());
        var otro = await cliente.GetFromJsonAsync<JsonElement>($"/api/notas-credito-compra?proveedorId={s.ProveedorId + 1}");
        Assert.Equal(0, otro.GetProperty("total").GetInt32());
        var hasta = await cliente.GetFromJsonAsync<JsonElement>($"/api/notas-credito-compra?emisionHasta=2026-11-04");
        Assert.Equal(0, hasta.GetProperty("total").GetInt32());
    }

    [PostgreSqlFact]
    public async Task Get_ConElRangoInvertido_400_YSinTokenEs401()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var s = await SembrarAsync(fixture);
        using var cliente = Cliente(fixture, "OPERADOR", s.UsuarioId);
        using var sinToken = Cliente(fixture, null);

        var respuesta = await cliente.GetAsync("/api/notas-credito-compra?emisionDesde=2026-11-30&emisionHasta=2026-11-01");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("emisión", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Unauthorized, (await sinToken.GetAsync("/api/notas-credito-compra")).StatusCode);
    }
}

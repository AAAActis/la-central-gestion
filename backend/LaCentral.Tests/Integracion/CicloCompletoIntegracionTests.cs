using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Moq;
using LaCentral.Api.Seguridad;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Proveedores.Dtos;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// HU-CLI-02 · HU-PRO-02 (tarea de integración del sprint): recorre de punta a
/// punta, contra el pipeline HTTP real (routing, DI, [Authorize], mapeo
/// Result→HTTP), el ciclo completo alta → consulta → modificación → baja →
/// reactivación de Cliente y de Proveedor.
///
/// Decisión de diseño: el repositorio se mockea en vez de conectar contra la
/// base real del contenedor, siguiendo el mismo criterio que ya usa
/// LoginIntegracionTests. Levantar Postgres desde acá no era viable en este
/// entorno, y mockear el puerto igual ejercita lo que este test necesita
/// probar: que el routing, la inyección de dependencias, la autorización y
/// el mapeo Result→HTTP funcionan juntos de punta a punta. Lo que NO cubre
/// es la traducción EF Core ↔ Postgres real, que ya tiene sus propios tests
/// de repositorio.
/// </summary>
public class CicloCompletoIntegracionTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private const string JwtKeyPrueba = "ClaveSecretaDePruebaMuyLargaParaQueNoFalle123!";
    private const string JwtIssuerPrueba = "LaCentralTests";
    private const string JwtAudiencePrueba = "LaCentralTestsAudience";

    public CicloCompletoIntegracionTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static string GenerarTokenDePrueba()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = JwtKeyPrueba,
                ["Jwt:Issuer"] = JwtIssuerPrueba,
                ["Jwt:Audience"] = JwtAudiencePrueba
            })
            .Build();

        // Reutilizamos el generador real de la API: el token de prueba queda
        // firmado exactamente igual que uno real, solo cambia la clave de origen.
        var generador = new GeneradorToken(config);
        return generador.GenerarToken(1, "test.integracion", "Operador", 1);
    }

    private HttpClient CrearClienteAutenticado(Action<IServiceCollection> configurarMocks)
    {
        var cliente = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = JwtKeyPrueba,
                    ["Jwt:Issuer"] = JwtIssuerPrueba,
                    ["Jwt:Audience"] = JwtAudiencePrueba
                });
            });

            builder.ConfigureTestServices(configurarMocks);
        }).CreateClient();

        cliente.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerarTokenDePrueba());

        return cliente;
    }

    // HU-CLI-01 → 02 → 03 → 04 → 05.
    //
    // Nota / hallazgo encontrado escribiendo este test: ni CrearClienteResponse
    // ni ClienteResumenDto ni ClienteDetalleDto exponen el Id numérico que
    // Modificar/Baja/Reactivar necesitan en la ruta ({id}). Contra la API real
    // no hay forma de obtenerlo después del alta o de una consulta — es un
    // hallazgo aparte que le paso a Javi, no algo que este test deba resolver.
    // Acá lo fijamos en un id conocido porque el repositorio está mockeado.
    [Fact]
    public async Task CicloDeVidaCompleto_Cliente_AltaConsultaModificacionBajaYReactivacion()
    {
        const int idClientePrueba = 1;
        const string codigoClientePrueba = "CLI-INT-01";
        Cliente? clienteGuardado = null;

        var repoMock = new Mock<IClienteRepositorio>();
        var contextoMock = new Mock<IContextoUsuario>();
        contextoMock.SetupGet(c => c.UsuarioId).Returns(1);
        contextoMock.SetupGet(c => c.NombreUsuario).Returns("test.integracion");
        contextoMock.SetupGet(c => c.SucursalId).Returns((short)1);

        repoMock.Setup(r => r.ExisteCodigoAsync(codigoClientePrueba, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => clienteGuardado != null);
        repoMock.Setup(r => r.ExisteCuitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoMock.Setup(r => r.ExisteRazonSocialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoMock.Setup(r => r.AgregarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((c, _) => clienteGuardado = c)
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.ObtenerDetallePorIdAsync(idClientePrueba, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => clienteGuardado);
        repoMock.Setup(r => r.ObtenerPorCuitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cliente?)null);
        repoMock.Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => clienteGuardado is null
                ? (IReadOnlyList<Cliente>)new List<Cliente>()
                : new List<Cliente> { clienteGuardado });
        repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()))
            .Callback<Cliente, CancellationToken>((c, _) => clienteGuardado = c)
            .Returns(Task.CompletedTask);

        var http = CrearClienteAutenticado(services =>
        {
            services.AddScoped(_ => repoMock.Object);
            services.AddScoped(_ => contextoMock.Object);
        });

        // 1. Alta — HU-CLI-01
        var altaRequest = new
        {
            Codigo = codigoClientePrueba,
            RazonSocial = "Cliente Integracion SA",
            Cuit = (string?)null,
            CondicionFiscal = "Responsable Inscripto",
            CondicionPago = "Contado",
            Telefonos = new List<string> { "3510000000" },
            Direcciones = new List<string> { "Bv. San Juan 500" }
        };
        var respuestaAlta = await http.PostAsJsonAsync("/api/clientes", altaRequest);
        Assert.Equal(HttpStatusCode.OK, respuestaAlta.StatusCode);
        Assert.NotNull(clienteGuardado);

        // 2. Consulta — HU-CLI-02
        var respuestaConsulta = await http.GetAsync(
            $"/api/clientes?texto={Uri.EscapeDataString("Cliente Integracion SA")}&incluirInactivos=false");
        Assert.Equal(HttpStatusCode.OK, respuestaConsulta.StatusCode);

        // 3. Detalle — HU-CLI-02, CA-005 (colecciones completas)
        var respuestaDetalle = await http.GetAsync($"/api/clientes/{idClientePrueba}");
        Assert.Equal(HttpStatusCode.OK, respuestaDetalle.StatusCode);
        var detalleContenido = await respuestaDetalle.Content.ReadAsStringAsync();
        Assert.Contains("3510000000", detalleContenido);

        // 4. Modificación — HU-CLI-03
        var modificarRequest = new
        {
            Codigo = codigoClientePrueba,
            RazonSocial = "Cliente Integracion SA Modificado",
            Cuit = (string?)null,
            CondicionFiscal = "Responsable Inscripto",
            CondicionPago = "Contado",
            Telefonos = new List<string> { "3510000000", "3511111111" },
            Direcciones = new List<string> { "Bv. San Juan 500" }
        };
        var respuestaModificar = await http.PutAsJsonAsync($"/api/clientes/{idClientePrueba}", modificarRequest);
        Assert.Equal(HttpStatusCode.NoContent, respuestaModificar.StatusCode);
        Assert.Equal("Cliente Integracion SA Modificado", clienteGuardado!.RazonSocial);
        Assert.Equal(2, clienteGuardado.Telefonos.Count);

        // 5. Baja — HU-CLI-04 (confirmación por Código, porque este cliente no tiene CUIT)
        var bajaRequest = new { Confirmacion = codigoClientePrueba, Motivo = "Prueba de integracion" };
        var respuestaBaja = await http.PostAsJsonAsync($"/api/clientes/{idClientePrueba}/baja", bajaRequest);
        Assert.Equal(HttpStatusCode.NoContent, respuestaBaja.StatusCode);
        Assert.False(clienteGuardado.Activo);
        Assert.Equal("Prueba de integracion", clienteGuardado.MotivoBaja);

        // 6. Reactivación — HU-CLI-05 (CA-003: el motivo de la baja anterior sigue como historial)
        var respuestaReactivar = await http.PostAsync($"/api/clientes/{idClientePrueba}/reactivacion", null);
        Assert.Equal(HttpStatusCode.NoContent, respuestaReactivar.StatusCode);
        Assert.True(clienteGuardado.Activo);
        Assert.Equal("Prueba de integracion", clienteGuardado.MotivoBaja);
    }

    // HU-PRO-01 → 02 → 03 → 04 → 05.
    //
    // A diferencia de Cliente, ProveedorResumenDto sí expone Id — así que acá
    // el id que se usa en Modificar/Baja/Reactivar sale de la respuesta real
    // de la consulta, no de una constante, igual que lo haría el frontend.
    [Fact]
    public async Task CicloDeVidaCompleto_Proveedor_AltaConsultaModificacionBajaYReactivacion()
    {
        var tabla = new List<Proveedor>();
        var siguienteId = 1;

        var repoMock = new Mock<IProveedorRepositorio>();

        repoMock.Setup(r => r.ExisteCuitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoMock.Setup(r => r.ExisteRazonSocialAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repoMock.Setup(r => r.AgregarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()))
            .Callback<Proveedor, CancellationToken>((p, _) =>
            {
                p.Id = siguienteId++;
                tabla.Add(p);
            })
            .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => (IReadOnlyList<Proveedor>)tabla.ToList());
        repoMock.Setup(r => r.ObtenerDetallePorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => tabla.FirstOrDefault(p => p.Id == id));
        repoMock.Setup(r => r.ObtenerPorCuitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Proveedor?)null);
        repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var http = CrearClienteAutenticado(services =>
        {
            services.AddScoped(_ => repoMock.Object);
        });

        // 1. Alta — HU-PRO-01
        var altaRequest = new
        {
            Codigo = "PROV-INT-01",
            RazonSocial = "Proveedor Integracion SA",
            Cuit = (string?)null,
            UrlReferencia = "https://proveedor-integracion.example.com",
            Telefonos = new List<string> { "3512222222" },
            Direcciones = new List<string> { "Av. Colon 1000" }
        };
        var respuestaAlta = await http.PostAsJsonAsync("/api/proveedores", altaRequest);
        Assert.Equal(HttpStatusCode.OK, respuestaAlta.StatusCode);
        Assert.Single(tabla);

        // 2. Consulta — HU-PRO-02 (de acá sale el Id real, como lo obtendría el frontend)
        var respuestaConsulta = await http.GetAsync(
            $"/api/proveedores?texto={Uri.EscapeDataString("Proveedor Integracion SA")}&incluirInactivos=false");
        Assert.Equal(HttpStatusCode.OK, respuestaConsulta.StatusCode);
        var resultados = await respuestaConsulta.Content.ReadFromJsonAsync<List<ProveedorResumenDto>>();
        Assert.NotNull(resultados);
        var idProveedor = Assert.Single(resultados!).Id;

        // 3. Modificación — HU-PRO-03
        var modificarRequest = new
        {
            RazonSocial = "Proveedor Integracion SA Modificado",
            Cuit = "30-99999999-9",
            UrlReferencia = "https://proveedor-integracion.example.com",
            Telefonos = new List<string> { "3512222222", "3513333333" },
            Direcciones = new List<string> { "Av. Colon 1000" }
        };
        var respuestaModificar = await http.PutAsJsonAsync($"/api/proveedores/{idProveedor}", modificarRequest);
        Assert.Equal(HttpStatusCode.NoContent, respuestaModificar.StatusCode);
        Assert.Equal("Proveedor Integracion SA Modificado", tabla.Single().RazonSocial);
        Assert.Equal(2, tabla.Single().Telefonos.Count);

        // 4. Baja — HU-PRO-04
        var bajaRequest = new { CuitReescrito = "30-99999999-9", MotivoBaja = "Prueba de integracion" };
        var respuestaBaja = await http.PostAsJsonAsync($"/api/proveedores/{idProveedor}/baja", bajaRequest);
        Assert.Equal(HttpStatusCode.NoContent, respuestaBaja.StatusCode);
        Assert.False(tabla.Single().Activo);
        Assert.Equal("Prueba de integracion", tabla.Single().MotivoBaja);

        // 5. Reactivación — HU-PRO-05
        var respuestaReactivar = await http.PostAsync($"/api/proveedores/{idProveedor}/reactivacion", null);
        Assert.Equal(HttpStatusCode.NoContent, respuestaReactivar.StatusCode);
        Assert.True(tabla.Single().Activo);
        Assert.Equal("Prueba de integracion", tabla.Single().MotivoBaja);
    }
}

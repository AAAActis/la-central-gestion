using LaCentral.Api.Seguridad;
using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>
/// 028 CA-002 (v1.2) y 016 CA-004 (v1.1) · D-S7-03 contra PostgreSQL real: el registro guarda el costo
/// previo en la línea y la anulación lo devuelve, con el caso de uso y los repositorios de producción.
/// La regla en sí está en CostoAlAnularTests. FALTA CORRER: necesitan el 13 aplicado en public de
/// lacentral_test (columnas costo_previo y costo_previo_guardado) y el cableado del repositorio, que va
/// con el re-scaffold del 13. Hasta entonces fallan con 42703 (columna inexistente).
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class CostoPrevioPostgreSqlTests
{
    private const string ClaveJefa = "clave-de-la-jefa";
    private readonly ITestOutputHelper _salida;
    public CostoPrevioPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    /// <summary>X con costo 50 y sin historial; F1 (P1) compra 2 de X a 100.</summary>
    private sealed record Escenario(PostgreSqlPruebas Fixture, int Operador, int P1, int X, short S1, int F1);

    private static async Task<Escenario> SembrarAsync(PostgreSqlPruebas fixture)
    {
        var operador = await fixture.CrearUsuarioAsync("operador.d-s7-03");
        int p1, x; short s1;
        await using (var contexto = fixture.CrearContexto())
        {
            s1 = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
            var jefa = new Usuario
            {
                NombreUsuario = "jefa.d-s7-03", HashContrasena = new ServicioHash().HashearClave(ClaveJefa),
                RolId = await contexto.Rol.Where(r => r.Nombre == "ADMINISTRADOR").Select(r => r.Id).SingleAsync(),
                SucursalId = s1, Activo = true
            };
            var proveedor = new Proveedor { Codigo = "PRV-CP", RazonSocial = "Proveedor costo previo", Activo = true };
            var articulo = new Articulo { CodigoInterno = "CP-X", Nombre = "Artículo costo previo", Activo = true, PrecioCosto = 50m };
            contexto.AddRange(jefa, proveedor, articulo);
            await contexto.SaveChangesAsync();
            (p1, x) = (proveedor.Id, articulo.Id);
        }
        var e = new Escenario(fixture, operador, p1, x, s1, 0);
        await using var c = fixture.CrearContexto();
        var registro = await new RegistrarFacturaCompraUseCase(new FacturaCompraRepositorio(c), new ProveedorRepositorio(c),
                new ArticuloRepositorio(c), new UnidadDeTrabajo(c), Sesion(e).Object, new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(c)))
            .EjecutarAsync(new RegistrarFacturaCompraRequest(p1, "A-0001-00000077", new DateOnly(2026, 10, 1), null, 0m, 0m, 0m, 0m,
                [new LineaFacturaCompraRequest(x, s1, 2m, 100m, 0m)], TipoComprobante: "A"));
        Assert.True(registro.IsSuccess, registro.Error);
        return e with { F1 = registro.Value.Id };
    }

    private static Mock<IContextoUsuario> Sesion(Escenario e)
    {
        var sesion = new Mock<IContextoUsuario>();
        sesion.SetupGet(s => s.UsuarioId).Returns(e.Operador);
        sesion.SetupGet(s => s.SucursalId).Returns(e.S1);
        return sesion;
    }

    private static async Task AnularAsync(Escenario e)
    {
        await using var c = e.Fixture.CrearContexto();
        var resultado = await new AnularFacturaCompraUseCase(new FacturaCompraRepositorio(c), new UsuarioRepositorio(c),
                new ServicioHash(), new UnidadDeTrabajo(c), Sesion(e).Object,
                new CalcularPreciosArticuloUseCase(new ArticuloRepositorio(c)), new NotaCreditoCompraRepositorio(c))
            .EjecutarAsync(e.F1, new AnularFacturaCompraRequest("Cargada dos veces", "jefa.d-s7-03", ClaveJefa));
        Assert.True(resultado.IsSuccess, resultado.Error);
    }

    private static async Task<decimal?> CostoAsync(Escenario e)
    {
        await using var c = e.Fixture.CrearContexto();
        return await c.Articulo.AsNoTracking().Where(a => a.Id == e.X).Select(a => a.PrecioCosto).SingleAsync();
    }

    [PostgreSqlFact]
    public async Task Registrar_GuardaEnLaLineaElCostoQueTeniaElArticuloJustoAntes()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);

        await using var c = fixture.CrearContexto();
        var linea = await c.Database.SqlQuery<LineaCostoPrevio>(
                $"SELECT costo_previo AS \"CostoPrevio\", costo_previo_guardado AS \"Guardado\" FROM factura_compra_detalle WHERE factura_compra_id = {e.F1}")
            .SingleAsync();
        Assert.Equal(new LineaCostoPrevio(50m, true), linea);
    }

    [PostgreSqlFact]
    public async Task CA002_Anular_SiNadieTocoElCosto_VuelveAlDeAntesDeLaFactura()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        Assert.Equal(100m, await CostoAsync(e));

        await AnularAsync(e);

        Assert.Equal(50m, await CostoAsync(e));
    }

    [PostgreSqlFact]
    public async Task CA002_Anular_SiDespuesSeCambioAMano_QuedaElDeAMano()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var e = await SembrarAsync(fixture);
        await using (var c = fixture.CrearContexto())
            await c.Articulo.Where(a => a.Id == e.X).ExecuteUpdateAsync(s => s.SetProperty(a => a.PrecioCosto, 130m));

        await AnularAsync(e);

        Assert.Equal(130m, await CostoAsync(e));
    }

    private sealed record LineaCostoPrevio(decimal? CostoPrevio, bool Guardado);
}

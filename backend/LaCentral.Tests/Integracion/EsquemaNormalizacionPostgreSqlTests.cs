using LaCentral.Data.Models;
using LaCentral.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;

namespace LaCentral.Tests.Integracion;

/// <summary>
/// Las afirmaciones del script 07 (normalización a 3FN y columnas de reportes) contra
/// PostgreSQL real. Lo que tiene que FALLAR se verifica con el código SQL y el nombre de la
/// restricción: un error cualquiera no prueba la regla. Lo que tiene que ENTRAR se relee
/// con un contexto nuevo.
/// </summary>
[Trait("Esquema", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class EsquemaNormalizacionPostgreSqlTests
{
    private const string ViolacionFk = "23503";
    private const string ViolacionUnica = "23505";
    private const string ViolacionCheck = "23514";

    private readonly ITestOutputHelper _salida;
    public EsquemaNormalizacionPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static async Task FallaConAsync(LaCentralDbContext contexto, string sqlState, string restriccion)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        var pg = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal((sqlState, restriccion), (pg.SqlState, pg.ConstraintName));
    }

    private static async Task<int> ProveedorAsync(PostgreSqlPruebas fixture, string codigo)
    {
        await using var contexto = fixture.CrearContexto();
        var proveedor = new Proveedor { Codigo = codigo, RazonSocial = $"Proveedor {codigo}", Activo = true };
        contexto.Proveedor.Add(proveedor);
        await contexto.SaveChangesAsync();
        return proveedor.Id;
    }

    private static async Task<int> ArticuloAsync(PostgreSqlPruebas fixture, string codigo, string? codigoBarras = null)
    {
        await using var contexto = fixture.CrearContexto();
        var articulo = new Articulo { CodigoInterno = codigo, Nombre = $"Artículo {codigo}", Activo = true, CodigoBarras = codigoBarras };
        contexto.Articulo.Add(articulo);
        await contexto.SaveChangesAsync();
        return articulo.Id;
    }

    private static FacturaCompra Factura(int proveedorId, int usuarioId, string numero) => new()
    {
        ProveedorId = proveedorId, Numero = numero, FechaEmision = new DateOnly(2026, 10, 1),
        Estado = "ACTIVA", Origen = "MANUAL", UsuarioId = usuarioId, FechaRegistro = DateTime.UtcNow
    };

    private static async Task<int> FacturaAsync(PostgreSqlPruebas fixture, int proveedorId, int usuarioId, string numero)
    {
        await using var contexto = fixture.CrearContexto();
        var factura = Factura(proveedorId, usuarioId, numero);
        contexto.FacturaCompra.Add(factura);
        await contexto.SaveChangesAsync();
        return factura.Id;
    }

    // Es el defecto que arregla la clave de tres columnas: antes, la segunda línea chocaba.
    [PostgreSqlFact]
    public async Task DetalleDeCompra_MismoArticuloEnDosSucursales_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var factura = await FacturaAsync(fixture, await ProveedorAsync(fixture, "PRV-1"), usuario, "A-1");
        var articulo = await ArticuloAsync(fixture, "ART-1");

        await using (var contexto = fixture.CrearContexto())
        {
            var sucursales = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).Take(2).ToArrayAsync();
            contexto.FacturaCompraDetalle.AddRange(sucursales.Select(s => new FacturaCompraDetalle
                { FacturaCompraId = factura, ArticuloId = articulo, SucursalId = s, Cantidad = 1m, PrecioUnitario = 10m }));
            await contexto.SaveChangesAsync();
        }

        await using var relectura = fixture.CrearContexto();
        Assert.Equal(2, await relectura.FacturaCompraDetalle.CountAsync(d => d.FacturaCompraId == factura && d.ArticuloId == articulo));
    }

    // La FK compuesta (factura_compra_id, proveedor_id): el historial no puede decir que lo
    // vendió un proveedor distinto del de su factura.
    [PostgreSqlFact]
    public async Task HistorialDeCompra_ConProveedorDistintoAlDeSuFactura_Falla()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var deLaFactura = await ProveedorAsync(fixture, "PRV-1");
        var otro = await ProveedorAsync(fixture, "PRV-2");
        var factura = await FacturaAsync(fixture, deLaFactura, usuario, "A-1");
        var articulo = await ArticuloAsync(fixture, "ART-1");

        await using var contexto = fixture.CrearContexto();
        contexto.ArticuloHistorialCompra.Add(new ArticuloHistorialCompra
            { ArticuloId = articulo, ProveedorId = otro, FacturaCompraId = factura, PrecioCosto = 10m, Fecha = DateTime.UtcNow });

        await FallaConAsync(contexto, ViolacionFk, "articulo_historial_compra_factura_proveedor_fkey");
    }

    // N-6 salió del 07 a propósito: condicion_pago no tiene CHECK hasta que Sergio cierre la lista.
    [PostgreSqlFact]
    public async Task CondicionDePago_CualquierValor_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        const string condicion = "FLOTA TAXI · 15 DÍAS FECHA FACTURA";
        int id;
        await using (var contexto = fixture.CrearContexto())
        {
            var cliente = new Cliente { Codigo = "CLI-1", RazonSocial = "Cliente 1", Activo = true, CondicionPago = condicion };
            contexto.Cliente.Add(cliente);
            await contexto.SaveChangesAsync();
            id = cliente.Id;
        }

        await using var relectura = fixture.CrearContexto();
        Assert.Equal(condicion, (await relectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == id)).CondicionPago);
    }

    [PostgreSqlFact]
    public async Task CodigoDeBarras_RepetidoEnDosArticulos_Falla()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await ArticuloAsync(fixture, "ART-1", "7791234567890");

        await using var contexto = fixture.CrearContexto();
        contexto.Articulo.Add(new Articulo { CodigoInterno = "ART-2", Nombre = "Artículo 2", Activo = true, CodigoBarras = "7791234567890" });

        await FallaConAsync(contexto, ViolacionUnica, "articulo_codigo_barras_unico");
    }

    // El UNIQUE es parcial: la mayoría de los artículos no tiene EAN.
    [PostgreSqlFact]
    public async Task CodigoDeBarras_NullEnDosArticulos_Entra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await ArticuloAsync(fixture, "ART-1");
        await ArticuloAsync(fixture, "ART-2");

        await using var relectura = fixture.CrearContexto();
        Assert.Equal(2, await relectura.Articulo.CountAsync(a => a.CodigoBarras == null));
    }

    [PostgreSqlFact]
    public async Task StockMinimo_Negativo_Falla()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var articulo = await ArticuloAsync(fixture, "ART-1");

        await using var contexto = fixture.CrearContexto();
        var sucursal = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
        contexto.Stock.Add(new Stock { ArticuloId = articulo, SucursalId = sucursal, Cantidad = 0m, Minimo = -1m });

        await FallaConAsync(contexto, ViolacionCheck, "stock_minimo_no_negativo");
    }

    // Doble certificación: quien autoriza la anulación no puede ser quien la pidió.
    [PostgreSqlFact]
    public async Task Anulacion_AutorizadaPorElMismoUsuarioQueLaPidio_Falla()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var proveedor = await ProveedorAsync(fixture, "PRV-1");

        await using var contexto = fixture.CrearContexto();
        var factura = Factura(proveedor, usuario, "A-1");
        factura.Estado = "ANULADA";
        factura.MotivoAnulacion = "Cargada por error";
        factura.FechaAnulacion = DateTime.UtcNow;
        factura.UsuarioAnulacionId = usuario;
        factura.UsuarioAutorizacionId = usuario;
        contexto.FacturaCompra.Add(factura);

        await FallaConAsync(contexto, ViolacionCheck, "factura_compra_autorizacion_distinta");
    }
}

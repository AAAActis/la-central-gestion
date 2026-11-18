using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using Xunit.Abstractions;

namespace LaCentral.Tests.FacturasCompra;

/// <summary>HU-FCO-05 (029) CA-004: el código del proveedor se busca en `articulo_codigo_alternativo`, contra PostgreSQL real.</summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class InterpretarFacturaPdfPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public InterpretarFacturaPdfPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task CodigosDelProveedor_SoloLosDeEseProveedorYDeArticulosActivos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int p1, activo;
        await using (var c = fixture.CrearContexto())
        {
            var prov1 = new Proveedor { Codigo = "PRV-PDF1", RazonSocial = "Ficticio Uno", Activo = true };
            var prov2 = new Proveedor { Codigo = "PRV-PDF2", RazonSocial = "Ficticio Dos", Activo = true };
            c.Proveedor.AddRange(prov1, prov2);
            await c.SaveChangesAsync();
            Articulo Articulo(string codigoInterno, int proveedor, string codigo, bool estaActivo = true) => new()
            {
                CodigoInterno = codigoInterno, Nombre = codigoInterno, Activo = estaActivo,
                MotivoBaja = estaActivo ? null : "Discontinuado", FechaBaja = estaActivo ? null : DateTime.UtcNow,
                ArticuloCodigoAlternativo = { new ArticuloCodigoAlternativo { ProveedorId = proveedor, Codigo = codigo } }
            };
            var a1 = Articulo("PDF-A1", prov1.Id, "C1");
            c.Articulo.AddRange(a1, Articulo("PDF-A2", prov2.Id, "C2"), Articulo("PDF-A3", prov1.Id, "C3", estaActivo: false));
            await c.SaveChangesAsync();
            (p1, activo) = (prov1.Id, a1.Id);
        }

        await using var nuevo = fixture.CrearContexto();
        var encontrados = await new FacturaCompraRepositorio(nuevo)
            .ObtenerArticulosPorCodigoProveedorAsync(p1, ["C1", "C2", "C3", "NO-EXISTE"]);

        Assert.Equal(new Dictionary<string, int> { ["C1"] = activo }, encontrados);
    }
}

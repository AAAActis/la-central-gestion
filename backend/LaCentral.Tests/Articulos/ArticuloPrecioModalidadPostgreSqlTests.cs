using System.Text.RegularExpressions;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Entidades;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Abstractions;
using Articulo = LaCentral.Data.Models.Articulo;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// D1: articulo_precio_modalidad guarda la modalidad como texto (el nombre exacto del
/// miembro del enum). Al leer, el texto se convierte en Modalidad y un valor fuera de
/// los seis FALLA: nunca se convierte en una modalidad válida ni en un default.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class ArticuloPrecioModalidadPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ArticuloPrecioModalidadPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static Articulo CrearArticulo(string codigo)
        => new() { CodigoInterno = codigo, Nombre = $"Artículo {codigo}", Activo = true, PrecioCosto = 100m };

    [PostgreSqlFact]
    public async Task Detalle_LeeLasSeisModalidadesPersistidasPorNombre()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuarioId = await fixture.CrearUsuarioAsync();
        var fecha = DateTime.UtcNow;
        var articulo = CrearArticulo("ART-D1-1");
        await using (var setup = fixture.CrearContexto())
        {
            setup.Articulo.Add(articulo);
            await setup.SaveChangesAsync();
            setup.ArticuloPrecioModalidad.AddRange(
                // Orden de inserción distinto del orden del enum, a propósito.
                new() { ArticuloId = articulo.Id, Modalidad = "CasoEspecial", Precio = 120m, CalculoManual = true, FechaCalculo = fecha, UsuarioId = usuarioId },
                new() { ArticuloId = articulo.Id, Modalidad = "Costo", Porcentaje = 0m, Precio = 100m, FechaCalculo = fecha },
                new() { ArticuloId = articulo.Id, Modalidad = "Tarjeta6" },
                new() { ArticuloId = articulo.Id, Modalidad = "EfectivoTransferencia", Porcentaje = 35m, Precio = 135m, FechaCalculo = fecha },
                new() { ArticuloId = articulo.Id, Modalidad = "Tarjeta3" },
                new() { ArticuloId = articulo.Id, Modalidad = "Lista", Porcentaje = 50m, Precio = 150m, FechaCalculo = fecha });
            await setup.SaveChangesAsync();
        }

        await using var lectura = fixture.CrearContexto();
        var detalle = await new ArticuloRepositorio(lectura).ObtenerDetallePorIdAsync(articulo.Id);

        Assert.NotNull(detalle);
        Assert.NotNull(detalle.PreciosPorModalidad);
        Assert.Equal(
            new[] { Modalidad.Lista, Modalidad.Tarjeta3, Modalidad.Tarjeta6, Modalidad.EfectivoTransferencia, Modalidad.Costo, Modalidad.CasoEspecial },
            detalle.PreciosPorModalidad.Select(p => p.Modalidad));
        Assert.Equal(new decimal?[] { 150m, null, null, 135m, 100m, 120m }, detalle.PreciosPorModalidad.Select(p => p.Valor));
    }

    [PostgreSqlFact]
    public async Task Detalle_ModalidadFueraDeLasSeis_FallaAlLeerEnVezDeConvertirseEnUnaValida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var esquema = fixture.CrearContexto())
        {
            // Para sembrar un valor inválido hay que quitar el CHECK, y SOLO en el esquema
            // aislado de esta prueba: search_path apunta únicamente a él.
            var actual = await esquema.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
            Assert.Matches(new Regex("^pr110_test_[0-9a-f]{32}$"), actual);
            await esquema.Database.ExecuteSqlRawAsync(
                "ALTER TABLE articulo_precio_modalidad DROP CONSTRAINT articulo_precio_modalidad_valida");
        }

        // "3" y " Lista" los acepta Enum.Parse; "lista" con ignoreCase; "" no es ninguna.
        var invalidos = new[] { "3", "lista", " Lista", "Lista ", "Mayorista", "" };
        var articulos = new Dictionary<string, int>();
        await using (var setup = fixture.CrearContexto())
        {
            foreach (var (valor, i) in invalidos.Select((v, i) => (v, i)))
            {
                var articulo = CrearArticulo($"ART-D1-INV-{i}");
                setup.Articulo.Add(articulo);
                await setup.SaveChangesAsync();
                setup.ArticuloPrecioModalidad.Add(new() { ArticuloId = articulo.Id, Modalidad = valor });
                await setup.SaveChangesAsync();
                articulos[valor] = articulo.Id;
            }
        }

        foreach (var valor in invalidos)
        {
            await using var lectura = fixture.CrearContexto();
            var repo = new ArticuloRepositorio(lectura);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repo.ObtenerDetallePorIdAsync(articulos[valor]));
            Assert.Contains($"'{valor}'", error.Message);
        }
    }

    [PostgreSqlFact]
    public async Task Esquema_RechazaModalidadFueraDeLasSeis()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var context = fixture.CrearContexto();
        var articulo = CrearArticulo("ART-D1-CHK");
        context.Articulo.Add(articulo);
        await context.SaveChangesAsync();

        context.ArticuloPrecioModalidad.Add(new() { ArticuloId = articulo.Id, Modalidad = "Mayorista" });
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var postgres = Assert.IsType<PostgresException>(error.InnerException);
        Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
        Assert.Equal("articulo_precio_modalidad_valida", postgres.ConstraintName);

        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.ArticuloPrecioModalidad.AnyAsync(p => p.ArticuloId == articulo.Id));
    }
}

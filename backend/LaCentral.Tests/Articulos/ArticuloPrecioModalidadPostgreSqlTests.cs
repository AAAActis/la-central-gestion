using System.Text.RegularExpressions;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos.Dtos;
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
        // Precisión de microsegundos, la de timestamptz: el valor releído tiene que ser igual.
        var fecha = new DateTime(2026, 10, 14, 12, 30, 15, DateTimeKind.Utc).AddTicks(1230);
        var manual = fecha.AddDays(-1);
        var articulo = CrearArticulo("ART-D1-1");
        await using (var setup = fixture.CrearContexto())
        {
            setup.Articulo.Add(articulo);
            await setup.SaveChangesAsync();
            setup.ArticuloPrecioModalidad.AddRange(
                // Orden de inserción distinto del orden del enum, a propósito.
                new() { ArticuloId = articulo.Id, Modalidad = "CasoEspecial", Precio = 120m, CalculoManual = true, FechaCalculo = manual, UsuarioId = usuarioId },
                new() { ArticuloId = articulo.Id, Modalidad = "Costo", Base = "CostoArticulo", Porcentaje = 0m, Precio = 100m, FechaCalculo = fecha },
                new() { ArticuloId = articulo.Id, Modalidad = "Tarjeta6" },
                new() { ArticuloId = articulo.Id, Modalidad = "EfectivoTransferencia", Base = "PrecioLista", Porcentaje = -10m, Precio = 135m, FechaCalculo = fecha },
                new() { ArticuloId = articulo.Id, Modalidad = "Tarjeta3" },
                new() { ArticuloId = articulo.Id, Modalidad = "Lista", Base = "CostoArticulo", Porcentaje = 50m, Precio = 150m, FechaCalculo = fecha });
            await setup.SaveChangesAsync();
        }

        await using var lectura = fixture.CrearContexto();
        var detalle = await new ArticuloRepositorio(lectura).ObtenerDetallePorIdAsync(articulo.Id);

        Assert.NotNull(detalle);
        Assert.NotNull(detalle.PreciosPorModalidad);
        // H3: cada columna por valor, no solo modalidad y precio.
        Assert.Equal(new PrecioModalidadConsultaDto[]
        {
            new(Modalidad.Lista, BasePrecio.CostoArticulo, 50m, 150m, false, fecha, null),
            new(Modalidad.Tarjeta3, null, null, null, false, null, null),
            new(Modalidad.Tarjeta6, null, null, null, false, null, null),
            new(Modalidad.EfectivoTransferencia, BasePrecio.PrecioLista, -10m, 135m, false, fecha, null),
            new(Modalidad.Costo, BasePrecio.CostoArticulo, 0m, 100m, false, fecha, null),
            new(Modalidad.CasoEspecial, null, null, 120m, true, manual, usuarioId),
        }, detalle.PreciosPorModalidad);
    }

    [PostgreSqlFact]
    public async Task Detalle_BaseFueraDeLasDos_FallaAlLeerEnVezDeConvertirseEnUnaValida()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var esquema = fixture.CrearContexto())
        {
            var actual = await esquema.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync();
            Assert.Matches(new Regex("^pr110_test_[0-9a-f]{32}$"), actual);
            await esquema.Database.ExecuteSqlRawAsync(
                "ALTER TABLE articulo_precio_modalidad DROP CONSTRAINT articulo_precio_modalidad_base_valida");
        }

        // "Costo" es la MODALIDAD, no la base: la trampa del nombre compartido.
        var invalidas = new[] { "Costo", "costoarticulo", " CostoArticulo", "1", "" };
        var articulos = new Dictionary<string, int>();
        await using (var setup = fixture.CrearContexto())
        {
            foreach (var (valor, i) in invalidas.Select((v, i) => (v, i)))
            {
                var articulo = CrearArticulo($"ART-D1-BASE-{i}");
                setup.Articulo.Add(articulo);
                await setup.SaveChangesAsync();
                setup.ArticuloPrecioModalidad.Add(new() { ArticuloId = articulo.Id, Modalidad = "Costo", Base = valor, Porcentaje = 0m });
                await setup.SaveChangesAsync();
                articulos[valor] = articulo.Id;
            }
        }

        foreach (var valor in invalidas)
        {
            await using var lectura = fixture.CrearContexto();
            var repo = new ArticuloRepositorio(lectura);
            var error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => repo.ObtenerDetallePorIdAsync(articulos[valor]));
            Assert.Contains($"'{valor}'", error.Message);
        }
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

    // Escribir no valida que el enum tenga nombre: los valores salen de miembros del enum
    // (ReglasPrecioModalidad.ConfiguracionInicial) y ningún DTO HTTP recibe una modalidad
    // ni una base. Si igual llegara uno sin nombre, se escribiría "99" o "9" y el motor lo
    // rechaza: no queda ni el artículo ni ninguna de sus filas.
    [PostgreSqlFact]
    public async Task Alta_ModalidadOBaseSinNombre_LaRechazaElCheckYNoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var casos = new (PrecioModalidad Precio, string Restriccion)[]
        {
            (new() { Modalidad = (Modalidad)99 }, "articulo_precio_modalidad_valida"),
            (new() { Modalidad = Modalidad.Costo, Base = (BasePrecio)9, Porcentaje = 0m }, "articulo_precio_modalidad_base_valida"),
        };

        foreach (var (precio, restriccion) in casos)
        {
            await using var contexto = fixture.CrearContexto();
            var articulo = new LaCentral.UseCases.Entidades.Articulo
            {
                Nombre = $"Artículo {restriccion}", Activo = true, Precios = new List<PrecioModalidad> { precio }
            };

            var error = await Assert.ThrowsAsync<DbUpdateException>(
                () => new ArticuloRepositorio(contexto).AgregarAsync(articulo));
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.CheckViolation, postgres.SqlState);
            Assert.Equal(restriccion, postgres.ConstraintName);
        }

        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.Articulo.AnyAsync());
        Assert.False(await relectura.ArticuloPrecioModalidad.AnyAsync());
    }
}

using LaCentral.Data;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using ArticuloDominio = LaCentral.UseCases.Entidades.Articulo;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-01 contra PostgreSQL real: caso de uso, repositorios y unidad de trabajo reales.
/// Todo se relee con un contexto nuevo.
/// </summary>
[Trait("Proveedor", "PostgreSQL")]
public class CrearArticuloPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public CrearArticuloPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static CrearArticuloUseCase Caso(LaCentralDbContext contexto, IArticuloRepositorio? articulos = null) =>
        new(articulos ?? new ArticuloRepositorio(contexto), new ArticuloRepositorio(contexto),
            new ProveedorRepositorio(contexto), new UnidadDeTrabajo(contexto));

    private static async Task<int> CrearProveedorAsync(PostgreSqlPruebas fixture, string codigo, bool activo = true)
    {
        await using var contexto = fixture.CrearContexto();
        var proveedor = new Proveedor
        {
            Codigo = codigo, RazonSocial = $"Proveedor {codigo}", Activo = activo,
            // proveedor_baja_coherente (D10): el inactivo exige motivo y fecha.
            MotivoBaja = activo ? null : "Dejó de operar", FechaBaja = activo ? null : DateTime.UtcNow
        };
        contexto.Proveedor.Add(proveedor);
        await contexto.SaveChangesAsync();
        return proveedor.Id;
    }

    /// <summary>
    /// El código provisional TMP-<guid> no puede sobrevivir: si AsignarCodigoInterno no pegara
    /// (ExecuteUpdate con 0 filas no falla), el artículo quedaría con él y el alta diría éxito.
    /// </summary>
    private static async Task SinCodigosProvisionalesAsync(PostgreSqlPruebas fixture)
    {
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.Articulo.AnyAsync(a => a.CodigoInterno.StartsWith("TMP-")));
    }

    private static async Task NoQuedoNadaAsync(PostgreSqlPruebas fixture, string nombre, int? articuloId = null)
    {
        await using var relectura = fixture.CrearContexto();
        await SinCodigosProvisionalesAsync(fixture);
        Assert.False(await relectura.Articulo.AnyAsync(a => a.Nombre == nombre));
        if (articuloId is { } id)
        {
            Assert.False(await relectura.Articulo.AnyAsync(a => a.Id == id));
            Assert.False(await relectura.ArticuloCodigoAlternativo.AnyAsync(c => c.ArticuloId == id));
            Assert.False(await relectura.Stock.AnyAsync(s => s.ArticuloId == id));
            Assert.False(await relectura.ArticuloPrecioModalidad.AnyAsync(p => p.ArticuloId == id));
        }
    }

    [PostgreSqlFact]
    public async Task Alta_PersisteArticuloCodigosStockYSeisPrecios()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");

        Result<CrearArticuloResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Caso(contexto).EjecutarAsync(new(" Bujía NGK BPR6ES ", " Estante 4 ",
                new CodigoProveedorRequest[] { new(norte, " bpr-6es "), new(sur, "ngk 4321") }));

        Assert.True(resultado.IsSuccess, resultado.Error);
        var id = resultado.Value.Id;
        Assert.True(id > 0);
        Assert.Equal($"LC-{id:D6}", resultado.Value.CodigoInterno);

        await using var relectura = fixture.CrearContexto();
        var articulo = await relectura.Articulo.AsNoTracking()
            .Include(a => a.ArticuloCodigoAlternativo).Include(a => a.Stock).Include(a => a.ArticuloPrecioModalidad)
            .SingleAsync(a => a.Id == id);
        Assert.Equal($"LC-{id:D6}", articulo.CodigoInterno);
        await SinCodigosProvisionalesAsync(fixture);
        Assert.Equal("Bujía NGK BPR6ES", articulo.Nombre);
        Assert.Equal("Estante 4", articulo.UbicacionDeposito);
        Assert.True(articulo.Activo);
        Assert.Null(articulo.PrecioCosto);
        Assert.Null(articulo.UltimoProveedorId);

        Assert.Equal(new[] { ("BPR-6ES", norte), ("NGK 4321", sur) },
            articulo.ArticuloCodigoAlternativo.OrderBy(c => c.ProveedorId).Select(c => (c.Codigo, c.ProveedorId)));

        var sucursales = await relectura.Sucursal.Select(s => s.Id).OrderBy(s => s).ToListAsync();
        Assert.NotEmpty(sucursales);
        Assert.Equal(sucursales.Select(s => (s, 0m)),
            articulo.Stock.OrderBy(s => s.SucursalId).Select(s => (s.SucursalId, s.Cantidad)));

        Assert.Equal(new (string, string?, decimal?, decimal?, bool, DateTime?, int?)[]
        {
            ("CasoEspecial", null, null, null, true, null, null),
            ("Costo", "CostoArticulo", 0m, null, false, null, null),
            ("EfectivoTransferencia", "PrecioLista", -10m, null, false, null, null),
            ("Lista", "CostoArticulo", 50m, null, false, null, null),
            ("Tarjeta3", null, null, null, false, null, null),
            ("Tarjeta6", null, null, null, false, null, null),
        }, articulo.ArticuloPrecioModalidad.OrderBy(p => p.Modalidad, StringComparer.Ordinal)
            .Select(p => (p.Modalidad, p.Base, p.Porcentaje, p.Precio, p.CalculoManual, p.FechaCalculo, p.UsuarioId)));
    }

    /// <summary>Delega en el repositorio real y falla DESPUÉS de que el alta escribió.</summary>
    private sealed class RepositorioQueFallaAlAsignarCodigo : IArticuloRepositorio
    {
        private readonly ArticuloRepositorio _real;
        private readonly LaCentralDbContext _contexto;
        private readonly bool _lanzar;
        public int? IdEscrito { get; private set; }
        public bool HabiaFilasAntesDeFallar { get; private set; }

        public RepositorioQueFallaAlAsignarCodigo(LaCentralDbContext contexto, bool lanzar)
            => (_real, _contexto, _lanzar) = (new ArticuloRepositorio(contexto), contexto, lanzar);

        public async Task<int> AgregarAsync(ArticuloDominio articulo, CancellationToken ct = default)
            => (IdEscrito = await _real.AgregarAsync(articulo, ct)).Value;

        public async Task<bool> AsignarCodigoInternoAsync(int id, string codigoInterno, CancellationToken ct = default)
        {
            // Dentro de la transacción las cuatro tablas ya tienen filas: la falla es a mitad de camino.
            HabiaFilasAntesDeFallar =
                await _contexto.Articulo.AnyAsync(a => a.Id == id, ct) &&
                await _contexto.ArticuloCodigoAlternativo.AnyAsync(c => c.ArticuloId == id, ct) &&
                await _contexto.Stock.AnyAsync(s => s.ArticuloId == id, ct) &&
                await _contexto.ArticuloPrecioModalidad.CountAsync(p => p.ArticuloId == id, ct) == 6;
            if (_lanzar) throw new InvalidOperationException("Falla forzada a mitad del alta.");
            return false;
        }

        public Task<IReadOnlyList<short>> ObtenerSucursalesAsync(CancellationToken ct = default)
            => _real.ObtenerSucursalesAsync(ct);

        // HU-ART-03: el alta no las usa; delegan en el real.
        public Task<bool> ActualizarDatosAsync(int id, string nombre, string? ubicacionDeposito, string? codigoInterno,
            CancellationToken ct = default) => _real.ActualizarDatosAsync(id, nombre, ubicacionDeposito, codigoInterno, ct);
        public Task AgregarCodigosAsync(int id, IReadOnlyList<LaCentral.UseCases.Entidades.CodigoAlternativo> codigos,
            CancellationToken ct = default) => _real.AgregarCodigosAsync(id, codigos, ct);
        public Task<bool> RetirarCodigoAsync(int id, int proveedorId, CancellationToken ct = default)
            => _real.RetirarCodigoAsync(id, proveedorId, ct);
        public Task<bool> DarDeBajaAsync(int id, string motivo, DateTime fechaBaja, CancellationToken ct = default)
            => _real.DarDeBajaAsync(id, motivo, fechaBaja, ct);
        public Task<bool> ReactivarAsync(int id, CancellationToken ct = default) => _real.ReactivarAsync(id, ct);
    }

    [PostgreSqlFact]
    public async Task Alta_ExcepcionAMitadDeCamino_NoQuedaArticuloNiCodigosNiStockNiPrecios()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");

        RepositorioQueFallaAlAsignarCodigo falla;
        await using (var contexto = fixture.CrearContexto())
        {
            falla = new RepositorioQueFallaAlAsignarCodigo(contexto, lanzar: true);
            await Assert.ThrowsAsync<InvalidOperationException>(() => Caso(contexto, falla)
                .EjecutarAsync(new("Artículo atómico", null, new CodigoProveedorRequest[] { new(norte, "ATOM-1") })));
        }

        Assert.True(falla.HabiaFilasAntesDeFallar);
        await NoQuedoNadaAsync(fixture, "Artículo atómico", falla.IdEscrito);
        await using var relectura = fixture.CrearContexto();
        Assert.False(await relectura.ArticuloCodigoAlternativo.AnyAsync(c => c.Codigo == "ATOM-1"));
    }

    [PostgreSqlFact]
    public async Task Alta_FalloDevueltoAMitadDeCamino_NoQuedaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");

        RepositorioQueFallaAlAsignarCodigo falla;
        Result<CrearArticuloResponse> resultado;
        await using (var contexto = fixture.CrearContexto())
        {
            falla = new RepositorioQueFallaAlAsignarCodigo(contexto, lanzar: false);
            resultado = await Caso(contexto, falla)
                .EjecutarAsync(new("Artículo atómico", null, new CodigoProveedorRequest[] { new(norte, "ATOM-2") }));
        }

        Assert.False(resultado.IsSuccess);
        Assert.True(falla.HabiaFilasAntesDeFallar);
        await NoQuedoNadaAsync(fixture, "Artículo atómico", falla.IdEscrito);
    }

    [PostgreSqlFact]
    public async Task D2_DosCodigosDelMismoProveedor_ConflictoYNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");

        await using (var contexto = fixture.CrearContexto())
        {
            var resultado = await Caso(contexto).EjecutarAsync(new("Artículo D2", null,
                new CodigoProveedorRequest[] { new(norte, "D2-A"), new(norte, "D2-B") }));
            Assert.Equal(TipoError.Conflicto, resultado.Tipo);
            Assert.Contains("Proveedor PRV-N", resultado.Error);
        }

        await NoQuedoNadaAsync(fixture, "Artículo D2");
    }

    [PostgreSqlFact]
    public async Task CodigoDeOtroArticulo_ConflictoInformandoCual()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var norte = await CrearProveedorAsync(fixture, "PRV-N");
        var sur = await CrearProveedorAsync(fixture, "PRV-S");
        await using (var setup = fixture.CrearContexto())
        {
            var existente = new Articulo { CodigoInterno = "MD14753", Nombre = "Mouse Genius", Activo = true };
            existente.ArticuloCodigoAlternativo.Add(new() { Codigo = "GN-01", ProveedorId = norte });
            setup.Articulo.Add(existente);
            await setup.SaveChangesAsync();
        }

        await using (var contexto = fixture.CrearContexto())
        {
            // Código alternativo de otro artículo, con otro proveedor y otra grafía.
            var porAlternativo = await Caso(contexto).EjecutarAsync(new("Nuevo 1", null,
                new CodigoProveedorRequest[] { new(sur, " gn-01 ") }));
            Assert.Equal(TipoError.Conflicto, porAlternativo.Tipo);
            Assert.Contains("MD14753", porAlternativo.Error);
            Assert.Contains("Mouse Genius", porAlternativo.Error);

            // Código interno de otro artículo usado como código de proveedor.
            var porInterno = await Caso(contexto).EjecutarAsync(new("Nuevo 2", null,
                new CodigoProveedorRequest[] { new(sur, "md14753") }));
            Assert.Equal(TipoError.Conflicto, porInterno.Tipo);
            Assert.Contains("Mouse Genius", porInterno.Error);
        }

        await NoQuedoNadaAsync(fixture, "Nuevo 1");
        await NoQuedoNadaAsync(fixture, "Nuevo 2");
    }

    [PostgreSqlFact]
    public async Task ProveedorInactivo_InvalidoYNoSeEscribe()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var inactivo = await CrearProveedorAsync(fixture, "PRV-X", activo: false);

        await using (var contexto = fixture.CrearContexto())
        {
            var resultado = await Caso(contexto).EjecutarAsync(new("Artículo con inactivo", null,
                new CodigoProveedorRequest[] { new(inactivo, "INA-1") }));
            Assert.Equal(TipoError.Invalido, resultado.Tipo);
            Assert.Contains("Proveedor PRV-X", resultado.Error);
        }

        await NoQuedoNadaAsync(fixture, "Artículo con inactivo");
    }

    // El patrón del Crítico 3: un ExecuteUpdate que no encuentra la fila devuelve 0 y no falla.
    // El repositorio real tiene que informarlo (false) y no tocar ninguna otra fila.
    [PostgreSqlFact]
    public async Task AsignarCodigoInterno_IdInexistente_DevuelveFalseYNoTocaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        int existente;
        await using (var setup = fixture.CrearContexto())
        {
            var articulo = new Articulo { CodigoInterno = "TMP-SIN-ASIGNAR", Nombre = "Provisional", Activo = true };
            setup.Articulo.Add(articulo);
            await setup.SaveChangesAsync();
            existente = articulo.Id;
        }

        await using (var contexto = fixture.CrearContexto())
            Assert.False(await new ArticuloRepositorio(contexto)
                .AsignarCodigoInternoAsync(existente + 1000, "LC-999999"));

        await using var relectura = fixture.CrearContexto();
        Assert.Equal("TMP-SIN-ASIGNAR", (await relectura.Articulo.SingleAsync(a => a.Id == existente)).CodigoInterno);
        Assert.False(await relectura.Articulo.AnyAsync(a => a.CodigoInterno == "LC-999999"));
    }

    // Sin MAX+1: dos altas a la vez, cada una con su conexión, no chocan.
    [PostgreSqlFact]
    public async Task DosAltasSimultaneas_CodigosInternosDistintos()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var fragueiro = fixture.CrearContexto();
        await using var sanVicente = fixture.CrearContexto();

        var resultados = await Task.WhenAll(
            Caso(fragueiro).EjecutarAsync(new("Alta desde Fragueiro", null, Array.Empty<CodigoProveedorRequest>())),
            Caso(sanVicente).EjecutarAsync(new("Alta desde San Vicente", null, Array.Empty<CodigoProveedorRequest>())));

        Assert.All(resultados, r => Assert.True(r.IsSuccess, r.Error));
        Assert.NotEqual(resultados[0].Value.CodigoInterno, resultados[1].Value.CodigoInterno);

        await using var relectura = fixture.CrearContexto();
        foreach (var r in resultados)
            Assert.Equal($"LC-{r.Value.Id:D6}", (await relectura.Articulo.SingleAsync(a => a.Id == r.Value.Id)).CodigoInterno);
        await SinCodigosProvisionalesAsync(fixture);
    }
}

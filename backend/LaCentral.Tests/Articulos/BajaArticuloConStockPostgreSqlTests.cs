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

namespace LaCentral.Tests.Articulos;

/// <summary>
/// 014 · CA-002 contra PostgreSQL real: con existencia no hay baja, ni en la fila ni en el
/// historial (D10). Se relee con un contexto nuevo.
/// </summary>
[Trait("Base", "PostgreSQL")]
public class BajaArticuloConStockPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public BajaArticuloConStockPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    private static DarDeBajaArticuloUseCase Baja(LaCentralDbContext contexto, int usuarioId, IConsultaArticulosRepositorio? consulta = null) =>
        new(new ArticuloRepositorio(contexto), consulta ?? new ArticuloRepositorio(contexto), new HistorialBajaRepositorio(contexto),
            new UnidadDeTrabajo(contexto), DoblesBaja.Contexto(usuarioId).Object);

    private static async Task<(int Id, string Codigo, short Sucursal)> CrearArticuloAsync(PostgreSqlPruebas fixture, decimal stock)
    {
        await using var contexto = fixture.CrearContexto();
        var sucursal = await contexto.Sucursal.OrderBy(s => s.Id).Select(s => s.Id).FirstAsync();
        var articulo = new Articulo { CodigoInterno = "BAJ-1", Nombre = "Bujía", Activo = true };
        if (stock != 0) articulo.Stock.Add(new Stock { SucursalId = sucursal, Cantidad = stock });
        contexto.Articulo.Add(articulo);
        await contexto.SaveChangesAsync();
        return (articulo.Id, articulo.CodigoInterno, sucursal);
    }

    private static async Task SinBajaAsync(PostgreSqlPruebas fixture, int id)
    {
        await using var relectura = fixture.CrearContexto();
        var fila = await relectura.Articulo.AsNoTracking().SingleAsync(a => a.Id == id);
        Assert.True(fila.Activo);
        Assert.Null(fila.MotivoBaja);
        Assert.False(await relectura.ArticuloHistorialBaja.AnyAsync(h => h.ArticuloId == id));
    }

    [PostgreSqlFact]
    public async Task Baja_ConExistencia_ConflictoYNoEscribeNiLaFilaNiElHistorial()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var (id, codigo, _) = await CrearArticuloAsync(fixture, stock: 3m);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Baja(contexto, usuario).EjecutarAsync(id, new DarDeBajaArticuloRequest("Discontinuado", codigo));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        await SinBajaAsync(fixture, id);
    }

    // La condición de existencia va en el UPDATE: vale lo que hay al escribir, no lo que se leyó.
    [PostgreSqlFact]
    public async Task Baja_ExistenciaQueEntraDespuesDeLeer_NoEscribeNiLaFilaNiElHistorial()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var (id, codigo, sucursal) = await CrearArticuloAsync(fixture, stock: 0m);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await Baja(contexto, usuario, new ConsultaQueSumaStockDespuesDeLeer(contexto, fixture, sucursal))
                .EjecutarAsync(id, new DarDeBajaArticuloRequest("Discontinuado", codigo));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        await SinBajaAsync(fixture, id);
    }

    [PostgreSqlFact]
    public async Task Baja_SinExistencia_EscribeLaFilaYElHistorial()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var usuario = await fixture.CrearUsuarioAsync();
        var (id, codigo, _) = await CrearArticuloAsync(fixture, stock: 0m);

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await Baja(contexto, usuario).EjecutarAsync(id, new DarDeBajaArticuloRequest("Discontinuado", codigo))).IsSuccess);

        await using var relectura = fixture.CrearContexto();
        Assert.False((await relectura.Articulo.AsNoTracking().SingleAsync(a => a.Id == id)).Activo);
        Assert.True(await relectura.ArticuloHistorialBaja.AnyAsync(h => h.ArticuloId == id && h.FechaReactivacion == null));
    }

    /// <summary>Lee con el repositorio real (sin stock) y, recién después, entra una compra en otra sesión.</summary>
    private sealed class ConsultaQueSumaStockDespuesDeLeer : IConsultaArticulosRepositorio
    {
        private readonly ArticuloRepositorio _real;
        private readonly PostgreSqlPruebas _fixture;
        private readonly short _sucursal;
        private bool _entro;

        public ConsultaQueSumaStockDespuesDeLeer(LaCentralDbContext contexto, PostgreSqlPruebas fixture, short sucursal)
            => (_real, _fixture, _sucursal) = (new ArticuloRepositorio(contexto), fixture, sucursal);

        public async Task<ArticuloDetalleConsultaDto?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default)
        {
            var leido = await _real.ObtenerDetallePorIdAsync(id, ct);
            if (_entro) return leido; // la compra entra una sola vez: las lecturas siguientes ven el stock
            _entro = true;
            await using var otraSesion = _fixture.CrearContexto();
            otraSesion.Stock.Add(new Stock { ArticuloId = id, SucursalId = _sucursal, Cantidad = 2m });
            await otraSesion.SaveChangesAsync(ct);
            return leido;
        }

        public Task<PaginaArticulosDto> BuscarAsync(ConsultaArticulos consulta, CancellationToken ct = default)
            => _real.BuscarAsync(consulta, ct);

        public Task<IReadOnlyList<ArticuloResumenConsultaDto>> ObtenerConflictosCodigoAsync(string codigo, CancellationToken ct = default)
            => _real.ObtenerConflictosCodigoAsync(codigo, ct);
    }
}

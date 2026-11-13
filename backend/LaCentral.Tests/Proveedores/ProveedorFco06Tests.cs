using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit.Abstractions;

namespace LaCentral.Tests.Proveedores;

/// <summary>
/// HU-FCO-06 (056) del lado del proveedor: el tipo de comprobante habitual (CA-004, solo un
/// valor por defecto) y la búsqueda por código o nombre sin CUIT, sin ceros a la izquierda
/// (CA-001 a CA-003). Lógica con Moq.
/// </summary>
public class ProveedorFco06Tests
{
    private readonly Mock<IProveedorRepositorio> _repo = new();

    public ProveedorFco06Tests() =>
        _repo.Setup(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

    private static CrearProveedorRequest Alta(string? tipo) =>
        new("PROV001", "Proveedor S.A.", null, null, null, null, tipo);

    private static ModificarProveedorRequest Modificacion(string? tipo) =>
        new("Proveedor Editado", "30-12345678-9", null, null, null, tipo);

    [Theory]
    [InlineData("C")]
    [InlineData("AB")]
    [InlineData("0")]
    public async Task Alta_TipoHabitualDistintoDeAyB_Invalido(string tipo)
    {
        var resultado = await new CrearProveedorUseCase(_repo.Object).EjecutarAsync(Alta(tipo));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repo.Verify(r => r.AgregarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("A", "A")]
    [InlineData(" b ", "B")]
    [InlineData(null, null)]
    [InlineData("  ", null)]
    public async Task Alta_TipoHabitual_SeGuardaNormalizadoONull(string? tipo, string? esperado)
    {
        Proveedor? agregado = null;
        _repo.Setup(r => r.AgregarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()))
            .Callback((Proveedor p, CancellationToken _) => agregado = p);

        var resultado = await new CrearProveedorUseCase(_repo.Object).EjecutarAsync(Alta(tipo));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(esperado, agregado!.TipoComprobanteHabitual);
    }

    [Fact]
    public async Task Modificacion_TipoHabitualDistintoDeAyB_Invalido()
    {
        _repo.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor { Id = 1, RazonSocial = "Original", Cuit = "30-12345678-9", Activo = true });

        var resultado = await new ModificarProveedorUseCase(_repo.Object).EjecutarAsync(1, Modificacion("X"));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _repo.Verify(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("B", "B")]
    [InlineData(null, null)]
    public async Task Modificacion_TipoHabitual_SeActualiza(string? tipo, string? esperado)
    {
        var proveedor = new Proveedor { Id = 1, RazonSocial = "Original", Cuit = "30-12345678-9", Activo = true, TipoComprobanteHabitual = "A" };
        _repo.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);

        var resultado = await new ModificarProveedorUseCase(_repo.Object).EjecutarAsync(1, Modificacion(tipo));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal(esperado, proveedor.TipoComprobanteHabitual);
    }

    [Fact]
    public async Task Consulta_DevuelveElTipoHabitual_YPasaPorCuitAlRepositorio()
    {
        _repo.Setup(r => r.BuscarAsync("35", false, 1, 20, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Proveedor> { new() { Id = 7, Codigo = "00035", RazonSocial = "Prov", Activo = true, TipoComprobanteHabitual = "B" } }, 1));

        var resultado = await new ConsultarProveedoresUseCase(_repo.Object).EjecutarAsync("35", false, porCuit: false);

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal("B", Assert.Single(resultado.Value.Items).TipoComprobanteHabitual);
    }
}

/// <summary>Lo mismo contra PostgreSQL real; toda afirmación relee con un contexto nuevo.</summary>
[Trait("Proveedor", "PostgreSQL")]
[Trait("Base", "PostgreSQL")]
public class ProveedorFco06PostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorFco06PostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task AltaYModificacion_TipoHabitual_QuedaEnLaFilaYEnElDetalle()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(new CrearProveedorRequest("00035", "Repuestos B", null, null, null, null, "B"))).IsSuccess);
        int id;
        await using (var lectura = fixture.CrearContexto())
        {
            var fila = await lectura.Proveedor.AsNoTracking().SingleAsync(p => p.Codigo == "00035");
            Assert.Equal('B', fila.TipoComprobanteHabitual);
            id = fila.Id;
        }

        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new ModificarProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(id, new ModificarProveedorRequest("Repuestos B", "30-12345678-9", null, null, null, "A"))).IsSuccess);

        await using var relectura = fixture.CrearContexto();
        Assert.Equal('A', (await relectura.Proveedor.AsNoTracking().SingleAsync(p => p.Id == id)).TipoComprobanteHabitual);
        var detalle = await new ObtenerProveedorDetalleUseCase(new ProveedorRepositorio(relectura)).EjecutarAsync(id);
        Assert.Equal("A", detalle.Value.TipoComprobanteHabitual);
    }

    [PostgreSqlFact]
    public async Task ElMotorRechazaUnTipoHabitualDistintoDeAyB()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using var contexto = fixture.CrearContexto();
        contexto.Proveedor.Add(new LaCentral.Data.Models.Proveedor
            { Codigo = "X1", RazonSocial = "X", Activo = true, TipoComprobanteHabitual = 'C' });

        var error = await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
        Assert.Equal("proveedor_tipo_comprobante_habitual_valido",
            Assert.IsType<Npgsql.PostgresException>(error.InnerException).ConstraintName);
    }

    private static async Task SembrarAsync(PostgreSqlPruebas fixture)
    {
        await using var contexto = fixture.CrearContexto();
        contexto.Proveedor.AddRange(
            new LaCentral.Data.Models.Proveedor { Codigo = "00035", RazonSocial = "Lubricantes del Sur", Cuit = "30-71234567-1", Activo = true, TipoComprobanteHabitual = 'B' },
            new LaCentral.Data.Models.Proveedor { Codigo = "350", RazonSocial = "Frenos Norte", Activo = true },
            new LaCentral.Data.Models.Proveedor { Codigo = "77", RazonSocial = "Distribuidora 35 Hermanos", Activo = true });
        await contexto.SaveChangesAsync();
    }

    // CA-003: 35 encuentra el 00035 (y no el 350); CA-001: también por nombre.
    [PostgreSqlFact]
    public async Task BuscarSinCuit_PorCodigoSinCerosALaIzquierda_ElCodigoExactoPrimero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await SembrarAsync(fixture);
        await using var contexto = fixture.CrearContexto();

        var (items, total) = await new ProveedorRepositorio(contexto).BuscarAsync("35", false, 1, 20, porCuit: false);

        Assert.True(total >= 1);
        Assert.Equal("00035", items[0].Codigo);
        Assert.Equal("B", items[0].TipoComprobanteHabitual);
        Assert.DoesNotContain(items, p => p.Codigo == "350");
    }

    [PostgreSqlFact]
    public async Task BuscarSinCuit_PorNombre_Encuentra()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await SembrarAsync(fixture);
        await using var contexto = fixture.CrearContexto();

        var (items, _) = await new ProveedorRepositorio(contexto).BuscarAsync("lubricantes sur", false, 1, 20, porCuit: false);

        Assert.Contains(items, p => p.Codigo == "00035");
    }

    // CA-001: la búsqueda por CUIT se retira de la factura de compra.
    [PostgreSqlFact]
    public async Task BuscarSinCuit_ConUnCuit_NoLoEncuentraPorCuit()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await SembrarAsync(fixture);
        await using var contexto = fixture.CrearContexto();
        var repositorio = new ProveedorRepositorio(contexto);

        var (sinCuit, _) = await repositorio.BuscarAsync("30-71234567-1", false, 1, 20, porCuit: false);
        var (conCuit, _) = await repositorio.BuscarAsync("30-71234567-1", false, 1, 20, porCuit: true);

        Assert.Empty(sinCuit);
        Assert.Equal("00035", Assert.Single(conCuit).Codigo);
    }
}

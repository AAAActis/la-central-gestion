using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// HU-ART-03 · retiro explícito de un código, baja y reactivación, con dobles. La
/// persistencia y la atomicidad se prueban contra PostgreSQL.
/// </summary>
public class BajaYCodigosArticuloUseCaseTests
{
    private const int Id = 42;
    private const int ProveedorNorte = 10;

    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();
    private readonly Mock<IHistorialBajaRepositorio> _historial = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();

    public BajaYCodigosArticuloUseCaseTests()
    {
        Articulo(activo: true);
        _articulos.Setup(r => r.RetirarCodigoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _articulos.Setup(r => r.DarDeBajaAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _articulos.Setup(r => r.ReactivarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void Articulo(bool activo) =>
        _consulta.Setup(c => c.ObtenerDetallePorIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloDetalleConsultaDto(Id, "LC-000042", "Bujía", activo, null, null,
                new[] { new CodigoProveedorConsultaDto(ProveedorNorte, "Distribuidora Norte", "bpr-6es", "BPR-6ES") },
                Array.Empty<StockSucursalConsultaDto>(), Array.Empty<PrecioModalidadConsultaDto>()));

    private void Rol(bool administrador) => _contexto.SetupGet(c => c.EsAdministrador).Returns(administrador);

    private RetirarCodigoProveedorUseCase Retirar() => new(_articulos.Object, _consulta.Object, _contexto.Object);

    private DarDeBajaArticuloUseCase Baja() =>
        new(_articulos.Object, _consulta.Object, _historial.Object, _unidad.Object, _contexto.Object);

    private ReactivarArticuloUseCase Reactivar() =>
        new(_articulos.Object, _consulta.Object, _historial.Object, _unidad.Object, _contexto.Object);

    private void NoSeRetiro() =>
        _articulos.Verify(r => r.RetirarCodigoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    private void NoSeDioDeBaja()
    {
        _articulos.Verify(r => r.DarDeBajaAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.Verify(h => h.AbrirAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void NoSeReactivo()
    {
        _articulos.Verify(r => r.ReactivarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.Verify(h => h.CerrarAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Retiro de un código: solo ADMINISTRADOR, operación explícita ----

    [Fact]
    public async Task Retirar_Operador_NoAutorizadoYNoRetira()
    {
        Rol(administrador: false);

        var resultado = await Retirar().EjecutarAsync(Id, ProveedorNorte);

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        NoSeRetiro();
    }

    [Fact]
    public async Task Retirar_Administrador_RetiraElCodigoDeEseProveedorPorId()
    {
        Rol(administrador: true);

        var resultado = await Retirar().EjecutarAsync(Id, ProveedorNorte);

        Assert.True(resultado.IsSuccess, resultado.Error);
        _articulos.Verify(r => r.RetirarCodigoAsync(Id, ProveedorNorte, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Retirar_ArticuloInexistente_NoEncontrado()
    {
        Rol(administrador: true);

        var resultado = await Retirar().EjecutarAsync(999, ProveedorNorte);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeRetiro();
    }

    // CA5: retirar un código también es modificarlo.
    [Fact]
    public async Task Retirar_ArticuloDadoDeBaja_InvalidoEIndicaQueHayQueReactivarlo()
    {
        Rol(administrador: true);
        Articulo(activo: false);

        var resultado = await Retirar().EjecutarAsync(Id, ProveedorNorte);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("reactiv", resultado.Error, StringComparison.OrdinalIgnoreCase);
        NoSeRetiro();
    }

    [Fact]
    public async Task Retirar_ProveedorSinCodigoEnElArticulo_NoEncontrado()
    {
        Rol(administrador: true);

        var resultado = await Retirar().EjecutarAsync(Id, 20);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeRetiro();
    }

    // Otra sesión lo retiró entre la lectura y el borrado.
    [Fact]
    public async Task Retirar_YaNoEstabaAlBorrar_NoEncontrado()
    {
        Rol(administrador: true);
        _articulos.Setup(r => r.RetirarCodigoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await Retirar().EjecutarAsync(Id, ProveedorNorte);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

    // ---- Baja lógica (D10) ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Baja_SinMotivo_Invalido(string motivo)
    {
        var resultado = await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest(motivo));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeDioDeBaja();
    }

    [Fact]
    public async Task Baja_Inexistente_NoEncontrado()
    {
        var resultado = await Baja().EjecutarAsync(999, new DarDeBajaArticuloRequest("Discontinuado"));

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeDioDeBaja();
    }

    [Fact]
    public async Task Baja_YaDadoDeBaja_Invalido()
    {
        Articulo(activo: false);

        var resultado = await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest("Discontinuado"));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeDioDeBaja();
    }

    [Fact]
    public async Task Baja_SinUsuarioEnSesion_NoAutorizado()
    {
        _contexto = DoblesBaja.Contexto(usuarioId: 0);

        var resultado = await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest("Discontinuado"));

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        NoSeDioDeBaja();
    }

    // La fila y su historial con el mismo motivo, la misma fecha y el usuario de la sesión.
    [Fact]
    public async Task Baja_EscribeLaFilaYAbreElHistorialConElUsuarioDeLaSesion()
    {
        var antes = DateTime.UtcNow;
        DateTime? fechaFila = null;
        _articulos.Setup(r => r.DarDeBajaAsync(Id, "Discontinuado", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback((int _, string _, DateTime f, CancellationToken _) => fechaFila = f)
            .ReturnsAsync(true);

        var resultado = await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest("  Discontinuado  "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(fechaFila);
        Assert.InRange(fechaFila.Value, antes, DateTime.UtcNow);
        _historial.Verify(h => h.AbrirAsync(EntidadConHistorialBaja.Articulo, Id, "Discontinuado", fechaFila.Value,
            DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Otra sesión lo dio de baja entre la lectura y la escritura: no se abre un historial.
    [Fact]
    public async Task Baja_YaNoEstabaActivoAlEscribir_InvalidoYNoAbreHistorial()
    {
        _articulos.Setup(r => r.DarDeBajaAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest("Discontinuado"));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _historial.Verify(h => h.AbrirAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Baja_EscriturasDentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Baja().EjecutarAsync(Id, new DarDeBajaArticuloRequest("Discontinuado"));

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeDioDeBaja();
    }

    // ---- Reactivación (D10) ----

    [Fact]
    public async Task Reactivar_Inexistente_NoEncontrado()
    {
        var resultado = await Reactivar().EjecutarAsync(999);

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeReactivo();
    }

    [Fact]
    public async Task Reactivar_YaActivo_Invalido()
    {
        var resultado = await Reactivar().EjecutarAsync(Id);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeReactivo();
    }

    [Fact]
    public async Task Reactivar_SinUsuarioEnSesion_NoAutorizado()
    {
        Articulo(activo: false);
        _contexto = DoblesBaja.Contexto(usuarioId: 0);

        var resultado = await Reactivar().EjecutarAsync(Id);

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        NoSeReactivo();
    }

    [Fact]
    public async Task Reactivar_ReactivaLaFilaYCierraElHistorialConElUsuarioDeLaSesion()
    {
        Articulo(activo: false);
        var antes = DateTime.UtcNow;

        var resultado = await Reactivar().EjecutarAsync(Id);

        Assert.True(resultado.IsSuccess, resultado.Error);
        _articulos.Verify(r => r.ReactivarAsync(Id, It.IsAny<CancellationToken>()), Times.Once);
        _historial.Verify(h => h.CerrarAsync(EntidadConHistorialBaja.Articulo, Id,
            It.Is<DateTime>(f => f >= antes && f <= DateTime.UtcNow), DoblesBaja.UsuarioEnSesion,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reactivar_YaNoEstabaInactivoAlEscribir_InvalidoYNoCierraHistorial()
    {
        Articulo(activo: false);
        _articulos.Setup(r => r.ReactivarAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await Reactivar().EjecutarAsync(Id);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _historial.Verify(h => h.CerrarAsync(It.IsAny<EntidadConHistorialBaja>(), It.IsAny<int>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reactivar_EscriturasDentroDeLaUnidadDeTrabajo()
    {
        Articulo(activo: false);
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Reactivar().EjecutarAsync(Id);

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeReactivo();
    }
}

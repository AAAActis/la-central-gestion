using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>
/// N-9 · la ubicación en depósito, por sucursal, desde la ficha. Lógica con dobles; la
/// escritura se prueba contra PostgreSQL en AsignarUbicacionPostgreSqlTests.
/// </summary>
public class AsignarUbicacionArticuloUseCaseTests
{
    private const int Id = 42;
    private const short Fragueiro = 1;

    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();

    public AsignarUbicacionArticuloUseCaseTests()
    {
        Articulo(activo: true);
        _articulos.Setup(r => r.ObtenerSucursalesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new short[] { 1, 2 });
        _articulos.Setup(r => r.AsignarUbicacionAsync(It.IsAny<int>(), It.IsAny<short>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private void Articulo(bool activo) =>
        _consulta.Setup(c => c.ObtenerDetallePorIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloDetalleConsultaDto(Id, "LC-000042", "Bujía", activo, null, null,
                Array.Empty<CodigoProveedorConsultaDto>(), Array.Empty<StockSucursalConsultaDto>(),
                Array.Empty<PrecioModalidadConsultaDto>()));

    private AsignarUbicacionArticuloUseCase Caso() => new(_articulos.Object, _consulta.Object);

    private void NoSeEscribioNada() =>
        _articulos.Verify(r => r.AsignarUbicacionAsync(It.IsAny<int>(), It.IsAny<short>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task UbicacionValida_EscribeRecortadaEnEsaSucursal()
    {
        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, "  Estante 4 ");

        Assert.True(resultado.IsSuccess, resultado.Error);
        _articulos.Verify(r => r.AsignarUbicacionAsync(Id, Fragueiro, "Estante 4", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task UbicacionEnBlanco_LaBorra(string? ubicacion)
    {
        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, ubicacion);

        Assert.True(resultado.IsSuccess, resultado.Error);
        _articulos.Verify(r => r.AsignarUbicacionAsync(Id, Fragueiro, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UbicacionDeMasDe60_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, new string('u', 61));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task UbicacionDe60_Entra()
    {
        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, new string('u', 60));

        Assert.True(resultado.IsSuccess, resultado.Error);
    }

    [Fact]
    public async Task ArticuloInexistente_NoEncontrado()
    {
        var resultado = await Caso().EjecutarAsync(999, Fragueiro, "Estante 4");

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task SucursalInexistente_NoEncontrado()
    {
        var resultado = await Caso().EjecutarAsync(Id, 9, "Estante 4");

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task DadoDeBaja_InvalidoSinEscribir()
    {
        Articulo(activo: false);

        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, "Estante 4");

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    // Otra sesión lo dio de baja entre la lectura y la escritura.
    [Fact]
    public async Task DadoDeBajaAlEscribir_Invalido()
    {
        _articulos.Setup(r => r.AsignarUbicacionAsync(It.IsAny<int>(), It.IsAny<short>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await Caso().EjecutarAsync(Id, Fragueiro, "Estante 4");

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
    }
}

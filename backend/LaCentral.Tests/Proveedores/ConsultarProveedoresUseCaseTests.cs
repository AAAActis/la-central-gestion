using Moq;
using Xunit;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Proveedores;

public class ConsultarProveedoresUseCaseTests
{
    private readonly Mock<IProveedorRepositorio> _mockRepo;
    private readonly ConsultarProveedoresUseCase _consultarUseCase;

    public ConsultarProveedoresUseCaseTests()
    {
        _mockRepo = new Mock<IProveedorRepositorio>();
        _consultarUseCase = new ConsultarProveedoresUseCase(_mockRepo.Object);
    }

    // CA-001: búsqueda por razón social difusa devuelve todas las coincidencias
    [Fact]
    public async Task Ejecutar_DosProveedoresMismaRazonSocial_DevuelveAmbos()
    {
        // Arrange
        var proveedoresSimulados = new List<Proveedor>
        {
            new Proveedor { Id = 1, Codigo = "PROV001", RazonSocial = "REPUESTOS CORDOBA", Activo = true },
            new Proveedor { Id = 2, Codigo = "PROV002", RazonSocial = "REPUESTOS CORDOBA", Activo = true }
        };

        _mockRepo.Setup(r => r.BuscarAsync("REPUESTOS CORDOBA", true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((proveedoresSimulados, proveedoresSimulados.Count));

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("REPUESTOS CORDOBA", incluirInactivos: true);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Value.Items.Count);
    }

    // CA-002: búsqueda por CUIT exacto devuelve a lo sumo uno, y propaga el Codigo
    [Fact]
    public async Task Ejecutar_BusquedaPorCuit_DevuelveExactamenteUnoConCodigo()
    {
        // Arrange
        var cuitBuscado = "30-12345678-9";
        var proveedoresSimulados = new List<Proveedor>
        {
            new Proveedor { Id = 10, Codigo = "PROV010", RazonSocial = "REPUESTOS DEL SUR", Cuit = cuitBuscado, Activo = true }
        };

        _mockRepo.Setup(r => r.BuscarAsync(cuitBuscado, false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((proveedoresSimulados, proveedoresSimulados.Count));

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync(cuitBuscado, incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Single(resultado.Value.Items);
        Assert.Equal(cuitBuscado, resultado.Value.Items.First().Cuit);
        // Codigo es la clave de negocio: el frontend la necesita en los resultados de búsqueda.
        Assert.Equal("PROV010", resultado.Value.Items.First().Codigo);
    }

    // CA-003: sin texto de búsqueda, rechaza antes de tocar el repositorio
    [Fact]
    public async Task Ejecutar_TextoVacio_DevuelveInvalidoSinConsultarRepositorio()
    {
        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("   ", incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _mockRepo.Verify(
            r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // CA-003: sin coincidencias, avisa explícitamente en vez de devolver una lista vacía
    [Fact]
    public async Task Ejecutar_SinCoincidencias_DevuelvePaginaVaciaConLaMismaEnvoltura()
    {
        // CA-003 v1.1 (#147, 05/11/2026): una búsqueda sin coincidencias es una consulta exitosa.
        _mockRepo.Setup(r => r.BuscarAsync("ASDXYZ", false, 1, 20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(((IReadOnlyList<Proveedor>)new List<Proveedor>(), 0));

        var resultado = await _consultarUseCase.EjecutarAsync("ASDXYZ", incluirInactivos: false);

        Assert.True(resultado.IsSuccess);
        Assert.Empty(resultado.Value.Items);
        Assert.Equal(0, resultado.Value.Total);
        Assert.Equal(1, resultado.Value.Pagina);
        Assert.Equal(20, resultado.Value.TamanoPagina);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)] // (página − 1) × tamaño no entra en un int: Skip desbordaría
    public async Task Ejecutar_PaginaOTamanoInvalido_DevuelveInvalidoSinConsultarRepositorio(int pagina, int tamanoPagina)
    {
        var resultado = await _consultarUseCase.EjecutarAsync("filtro", false, pagina, tamanoPagina);

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        _mockRepo.Verify(r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // CA-004: el filtro de activos/inactivos se delega en el repositorio, no se resuelve en memoria
    [Fact]
    public async Task Ejecutar_SinIncluirInactivos_LlamaAlRepositorioConBanderaEnFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync(It.IsAny<string>(), false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(((IReadOnlyList<Proveedor>)new List<Proveedor>(), 0));

        // Act
        await _consultarUseCase.EjecutarAsync("filtro", incluirInactivos: false);

        // Assert
        _mockRepo.Verify(r => r.BuscarAsync("filtro", false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

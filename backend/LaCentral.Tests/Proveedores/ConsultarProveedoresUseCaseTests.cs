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

        _mockRepo.Setup(r => r.BuscarAsync("REPUESTOS CORDOBA", true, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(proveedoresSimulados);

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("REPUESTOS CORDOBA", incluirInactivos: true);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Value.Count);
    }

    // CA-002: búsqueda por CUIT exacto devuelve a lo sumo uno
    [Fact]
    public async Task Ejecutar_BusquedaPorCuit_DevuelveExactamenteUno()
    {
        // Arrange
        var cuitBuscado = "30-12345678-9";
        var proveedoresSimulados = new List<Proveedor>
        {
            new Proveedor { Id = 10, Codigo = "PROV010", RazonSocial = "REPUESTOS DEL SUR", Cuit = cuitBuscado, Activo = true }
        };

        _mockRepo.Setup(r => r.BuscarAsync(cuitBuscado, false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(proveedoresSimulados);

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync(cuitBuscado, incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Single(resultado.Value);
        Assert.Equal(cuitBuscado, resultado.Value.First().Cuit);
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
            r => r.BuscarAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // CA-003: sin coincidencias, avisa explícitamente en vez de devolver una lista vacía
    [Fact]
    public async Task Ejecutar_SinCoincidencias_DevuelveNoEncontradoConMensajeExplicito()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync("ASDXYZ", false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Proveedor>());

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("ASDXYZ", incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        Assert.Equal("No se encontraron proveedores que coincidan con la búsqueda.", resultado.Error);
    }

    // CA-004: el filtro de activos/inactivos se delega en el repositorio, no se resuelve en memoria
    [Fact]
    public async Task Ejecutar_SinIncluirInactivos_LlamaAlRepositorioConBanderaEnFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Proveedor>());

        // Act
        await _consultarUseCase.EjecutarAsync("filtro", incluirInactivos: false);

        // Assert
        _mockRepo.Verify(r => r.BuscarAsync("filtro", false, It.IsAny<CancellationToken>()), Times.Once);
    }
}

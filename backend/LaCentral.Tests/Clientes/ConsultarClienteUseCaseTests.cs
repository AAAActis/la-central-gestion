using Moq;
using Xunit;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Clientes.Dtos;

namespace LaCentral.Tests.Clientes;
public class ConsultarClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _mockRepo;
    private readonly ConsultarClientesUseCase _consultarUseCase;
    private readonly ObtenerClienteDetalleUseCase _obtenerUseCase;

    public ConsultarClienteUseCaseTests()
    {
        _mockRepo = new Mock<IClienteRepositorio>();
        _consultarUseCase = new ConsultarClientesUseCase(_mockRepo.Object);
        _obtenerUseCase = new ObtenerClienteDetalleUseCase(_mockRepo.Object);
    }

    // CA-001
    [Fact]
    public async Task Ejecutar_DosClientesMismaRazonSocial_DevuelveAmbos()
    {
        // Arrange
        var clientesSimulados = new List<Cliente>
        {
            new Cliente { Codigo = "001", RazonSocial = "REPUESTOS CORDOBA" },
            new Cliente { Codigo = "002", RazonSocial = "REPUESTOS CORDOBA" }
        };
        
        _mockRepo.Setup(r => r.BuscarAsync("REPUESTOS CORDOBA", true, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(clientesSimulados);

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("REPUESTOS CORDOBA", incluirInactivos: true);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Value.Count);
    }

    // CA-002
    [Fact]
    public async Task Ejecutar_BusquedaPorCuit_DevuelveExactamenteUno()
    {
        // Arrange
        var cuitBuscado = "30-12345678-9";
        var clientesSimulados = new List<Cliente>
        {
            new Cliente { Codigo = "010", RazonSocial = "TALLER SAN VICENTE", Cuit = cuitBuscado }
        };

        _mockRepo.Setup(r => r.BuscarAsync(cuitBuscado, false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(clientesSimulados);

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync(cuitBuscado, incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Single(resultado.Value);
        Assert.Equal(cuitBuscado, resultado.Value.First().Cuit);
    }

    // CA-003
    [Fact]
    public async Task Ejecutar_SinCoincidencias_DevuelveFalloConMensajeExplicito()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync("ASDXYZ", false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Cliente>()); // Lista vacía

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("ASDXYZ", incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsFailure);
        Assert.Equal("No se encontraron clientes que coincidan con la búsqueda.", resultado.Error); // Ajustá el string al mensaje exacto de tu UseCase
    }

    // CA-004
    [Fact]
    public async Task Ejecutar_SinIncluirInactivos_LlamaAlRepositorioConBanderaEnFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync(It.IsAny<string>(), false, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new List<Cliente>()); 

        // Act
        await _consultarUseCase.EjecutarAsync("filtro", incluirInactivos: false);

        // Assert
        // Verificamos que el UseCase le pasó correctamente el false al puerto, 
        // delegando la responsabilidad de filtrar a la base de datos.
        _mockRepo.Verify(r => r.BuscarAsync("filtro", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-005 (Usando el UseCase de Detalle de Santi)
    [Fact]
    public async Task Ejecutar_DetalleCliente_TraeTodosLosTelefonosYDirecciones()
    {
        // Arrange
        var clienteConColecciones = new Cliente
        {
            Codigo = "100",
            RazonSocial = "CLIENTE COMPLETO",
            Activo = true,
            Telefonos = new List<string> { "3510000000", "3511111111" },
            Direcciones = new List<string> { "Av. Colón 1000", "Bv. San Juan 500" }
        };

        _mockRepo.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(clienteConColecciones);

        // Act
        var resultado = await _obtenerUseCase.EjecutarAsync(1);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Value.Telefonos.Count);
        Assert.Equal(2, resultado.Value.Direcciones.Count);
        Assert.Contains("3510000000", resultado.Value.Telefonos);
        Assert.Contains("Av. Colón 1000", resultado.Value.Direcciones);
    }

}
using LaCentral.UseCases.Comun;
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
        
        _mockRepo.Setup(r => r.BuscarAsync("REPUESTOS CORDOBA", true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((clientesSimulados, clientesSimulados.Count));

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync("REPUESTOS CORDOBA", incluirInactivos: true);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Equal(2, resultado.Value.Items.Count);
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

        _mockRepo.Setup(r => r.BuscarAsync(cuitBuscado, false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((clientesSimulados, clientesSimulados.Count));

        // Act
        var resultado = await _consultarUseCase.EjecutarAsync(cuitBuscado, incluirInactivos: false);

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.Single(resultado.Value.Items);
        Assert.Equal(cuitBuscado, resultado.Value.Items.First().Cuit);
    }

    // CA-003
    [Fact]
    public async Task Ejecutar_SinCoincidencias_DevuelvePaginaVaciaConLaMismaEnvoltura()
    {
        // CA-003 v1.1 (#147, 05/11/2026): una búsqueda sin coincidencias es una consulta exitosa.
        _mockRepo.Setup(r => r.BuscarAsync("ASDXYZ", false, 1, 20, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(((IReadOnlyList<Cliente>)new List<Cliente>(), 0));

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

    // CA-004
    [Fact]
    public async Task Ejecutar_SinIncluirInactivos_LlamaAlRepositorioConBanderaEnFalse()
    {
        // Arrange
        _mockRepo.Setup(r => r.BuscarAsync(It.IsAny<string>(), false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(((IReadOnlyList<Cliente>)new List<Cliente>(), 0)); 

        // Act
        await _consultarUseCase.EjecutarAsync("filtro", incluirInactivos: false);

        // Assert
        // Verificamos que el UseCase le pasó correctamente el false al puerto, 
        // delegando la responsabilidad de filtrar a la base de datos.
        _mockRepo.Verify(r => r.BuscarAsync("filtro", false, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
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
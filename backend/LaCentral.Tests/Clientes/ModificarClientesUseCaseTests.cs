using Moq;
using Xunit;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Clientes;

public class ModificarClienteUseCaseTests
{
    private readonly Mock<IClienteRepositorio> _repoMock = new();

    private ModificarClienteUseCase CrearCasoDeUso() => new(_repoMock.Object);

    private ModificarClienteRequest CrearRequestValido() => new(
        "CLI-001", "Cliente Editado", "30-12345678-9", "RI", "Contado", null, null);

    private Cliente CrearClienteBdActivo() => new()
    {
        Codigo = "CLI-001", RazonSocial = "Original", Cuit = "30-00000000-0", Activo = true
    };

    [Fact]
    public async Task Ejecutar_ClienteInactivo_DevuelveInvalido()
    {
        var clienteInactivo = CrearClienteBdActivo();
        clienteInactivo.Activo = false;

        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(clienteInactivo);

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("dado de baja", resultado.Error);
    }

    [Fact]
    public async Task Ejecutar_CuitDeOtroCliente_DevuelveConflicto()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CrearClienteBdActivo());

        _repoMock.Setup(r => r.ObtenerPorCuitAsync("30-12345678-9", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Cliente { Codigo = "CLI-002", RazonSocial = "Otro Cliente", Cuit = "30-12345678-9" });

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("ya pertenece", resultado.Error);
    }
}
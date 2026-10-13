using Moq;
using Xunit;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Proveedores;

public class ModificarProveedorUseCaseTests
{
    private readonly Mock<IProveedorRepositorio> _repoMock = new();

    private ModificarProveedorUseCase CrearCasoDeUso() => new(_repoMock.Object);

    private ModificarProveedorRequest CrearRequestValido() => new(
        "Proveedor Editado", "30-12345678-9", "https://proveedor.com", null, null);

    private Proveedor CrearProveedorBdActivo() => new()
    {
        Id = 1, RazonSocial = "Original", Cuit = "30-00000000-0", Activo = true
    };

    // CA-004: Proveedor inactivo deniega la modificación
    [Fact]
    public async Task Ejecutar_ProveedorInactivo_DevuelveInvalido()
    {
        var proveedorInactivo = CrearProveedorBdActivo();
        proveedorInactivo.Activo = false;

        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(proveedorInactivo);

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("dado de baja", resultado.Error);
    }

    // CA-002: CUIT duplicado de otro proveedor
    [Fact]
    public async Task Ejecutar_CuitDeOtroProveedor_DevuelveConflicto()
    {
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(CrearProveedorBdActivo());

        // El CUIT del request ya pertenece al proveedor ID 2
        _repoMock.Setup(r => r.ObtenerPorCuitAsync("30-12345678-9", It.IsAny<CancellationToken>()))
                 .ReturnsAsync(new Proveedor { Id = 2, RazonSocial = "Otro Proveedor", Cuit = "30-12345678-9" });

        var caso = CrearCasoDeUso();
        var resultado = await caso.EjecutarAsync(1, CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("ya pertenece", resultado.Error);
    }
}
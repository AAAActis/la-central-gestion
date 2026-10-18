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

    public ModificarProveedorUseCaseTests() => _repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

    private ModificarProveedorUseCase CrearCasoDeUso() => new(_repoMock.Object);

    private ModificarProveedorRequest CrearRequestValido() => new(
        "Proveedor Editado", "30-12345678-9", "https://proveedor.com", null, null);

    private Proveedor CrearProveedorBdActivo() => new()
    {
        Id = 1, RazonSocial = "Original", Cuit = "30-00000000-0", Activo = true
    };

// CRÍTICO 3 en proveedor: la fila desaparece entre la lectura y la escritura.
    [Fact]
    public async Task Ejecutar_FilaInexistenteAlActualizar_DevuelveNoEncontrado()
    {
        var proveedor = CrearProveedorBdActivo();
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);
        _repoMock.Setup(r => r.ActualizarAsync(proveedor, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var resultado = await CrearCasoDeUso().EjecutarAsync(1, CrearRequestValido());

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
    }

// Una dirección sin calle llega como "" (así la lee el repositorio): filtrarla la borraba con su localidad.
    [Fact]
    public async Task Ejecutar_DireccionSinCalle_SeConservaParaElRepositorio()
    {
        var proveedor = CrearProveedorBdActivo();
        proveedor.Direcciones = new List<string> { "", "Ruta 9 km 5" };
        _repoMock.Setup(r => r.ObtenerDetallePorIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);
        var request = CrearRequestValido() with { Cuit = proveedor.Cuit!, Direcciones = new List<string> { "", "Ruta 9 km 5" } };

        var resultado = await CrearCasoDeUso().EjecutarAsync(1, request);

        Assert.True(resultado.IsSuccess);
        _repoMock.Verify(r => r.ActualizarAsync(
            It.Is<Proveedor>(p => p.Direcciones.SequenceEqual(new[] { "", "Ruta 9 km 5" })), It.IsAny<CancellationToken>()), Times.Once);
    }

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
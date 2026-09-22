using Moq;
using Xunit;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.Tests.Proveedores;

public class CrearProveedorUseCaseTests
{
    private readonly Mock<IProveedorRepositorio> _repoMock = new();

    private CrearProveedorUseCase CrearCasoDeUso() => new(_repoMock.Object);

    private CrearProveedorRequest CrearRequestValido() => new(
        "Proveedor S.A.", "30-12345678-9", "https://proveedor.com", null, null);

    // CA-004: URL inválida bloquea el alta
    [Fact]
    public async Task Ejecutar_UrlInvalida_DevuelveInvalido()
    {
        var request = CrearRequestValido() with { UrlReferencia = "www.sin-http.com" };
        var caso = CrearCasoDeUso();

        var resultado = await caso.EjecutarAsync(request);

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("formato válido", resultado.Error);
    }

    // CA-005: Sin URL registra igual
    [Fact]
    public async Task Ejecutar_SinUrl_RegistraExitosamente()
    {
        var request = CrearRequestValido() with { UrlReferencia = null };
        var caso = CrearCasoDeUso();

        var resultado = await caso.EjecutarAsync(request);

        Assert.True(resultado.IsSuccess);
        _repoMock.Verify(r => r.AgregarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // CA-002: CUIT duplicado rechaza
    [Fact]
    public async Task Ejecutar_CuitDuplicado_DevuelveConflicto()
    {
        _repoMock.Setup(r => r.ExisteCuitAsync("30-12345678-9", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = CrearCasoDeUso();

        var resultado = await caso.EjecutarAsync(CrearRequestValido());

        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
    }

    // CA-003: Razón social repetida genera advertencia
    [Fact]
    public async Task Ejecutar_RazonSocialRepetida_RegistraConAdvertencia()
    {
        _repoMock.Setup(r => r.ExisteRazonSocialAsync("Proveedor S.A.", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = CrearCasoDeUso();

        var resultado = await caso.EjecutarAsync(CrearRequestValido());

        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value.Advertencia);
    }
}
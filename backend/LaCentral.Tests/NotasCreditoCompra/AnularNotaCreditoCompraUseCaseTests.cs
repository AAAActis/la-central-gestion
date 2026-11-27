using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.NotasCreditoCompra;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.NotasCreditoCompra;

/// <summary>
/// HU-FCO-07 (057) CA-012 (D-S6-23) · anulación de la nota, con dobles. Que el stock vuelva al
/// origen en la misma transacción se prueba contra PostgreSQL.
/// </summary>
public class AnularNotaCreditoCompraUseCaseTests
{
    private const int NotaVigente = 70;
    private const int NotaAnulada = 71;

    private readonly Mock<INotaCreditoCompraRepositorio> _notas = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();
    private AnulacionNotaCreditoCompra? _anulacion;

    public AnularNotaCreditoCompraUseCaseTests()
    {
        _notas.Setup(n => n.ObtenerParaAnularAsync(NotaVigente, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotaCreditoParaAnular(NotaVigente, "0010-00021495", true));
        _notas.Setup(n => n.ObtenerParaAnularAsync(NotaAnulada, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotaCreditoParaAnular(NotaAnulada, "0010-00021496", false));
        _notas.Setup(n => n.AnularConEfectosAsync(It.IsAny<AnulacionNotaCreditoCompra>(), It.IsAny<CancellationToken>()))
            .Callback((AnulacionNotaCreditoCompra a, CancellationToken _) => _anulacion = a)
            .ReturnsAsync(Result.Success());
        _contexto.SetupGet(c => c.EsAdministrador).Returns(true);
    }


    private AnularNotaCreditoCompraUseCase Caso() =>
        new(_notas.Object, _unidad.Object, _contexto.Object);

    private static AnularNotaCreditoCompraRequest Pedido(string? motivo = " Mal cargada ") => new(motivo);

    private async Task Rechaza(int notaId, AnularNotaCreditoCompraRequest pedido, TipoError tipo)
    {
        var resultado = await Caso().EjecutarAsync(notaId, pedido);

        Assert.Equal((false, tipo), (resultado.IsSuccess, resultado.Tipo));
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
        _notas.Verify(n => n.AnularConEfectosAsync(It.IsAny<AnulacionNotaCreditoCompra>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task SinMotivo_Invalido(string? motivo) => await Rechaza(NotaVigente, Pedido(motivo: motivo), TipoError.Invalido);

    [Fact]
    public async Task NotaInexistente_NoEncontrado() => await Rechaza(999, Pedido(), TipoError.NoEncontrado);

    [Fact]
    public async Task NotaYaAnulada_Conflicto() => await Rechaza(NotaAnulada, Pedido(), TipoError.Conflicto);

    [Fact]
    public async Task SinUsuarioEnLaSesion_NoAutorizado()
    {
        _contexto = DoblesBaja.Contexto(0);

        await Rechaza(NotaVigente, Pedido(), TipoError.NoAutorizado);
    }

    // CA-012 (v1.3, D-S7-04): un ADMINISTRADOR la anula solo, sin autorización de otra persona.
    [Fact]
    public async Task CA012_UnAdministradorLaAnulaSolo()
    {
        var resultado = await Caso().EjecutarAsync(NotaVigente, new AnularNotaCreditoCompraRequest(" Mal cargada "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal((NotaVigente, "Mal cargada", DoblesBaja.UsuarioEnSesion), (_anulacion!.NotaId, _anulacion.Motivo, _anulacion.UsuarioId));
        Assert.True((DateTime.UtcNow - _anulacion.Fecha).Duration() < TimeSpan.FromMinutes(1));
    }

    // CA-012 (v1.3): si quien la pide no es ADMINISTRADOR, 403 y no cambia nada.
    [Fact]
    public async Task CA012_UnOperador_Prohibido()
    {
        _contexto = DoblesBaja.Contexto(10);
        await Rechaza(NotaVigente, Pedido(), TipoError.Prohibido);
    }

    [Fact]
    public async Task LaAnulacionCorreDentroDeLaUnidadDeTrabajo_YDevuelveSuFallo()
    {
        _unidad = new Mock<IUnidadDeTrabajo>();
        _unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(TipoError.Conflicto, "no corrió"));

        await Rechaza(NotaVigente, Pedido(), TipoError.Conflicto);
    }
}

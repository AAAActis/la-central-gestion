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
    private const int Administrador = 2;
    private const int Operador = 1;

    private readonly Mock<INotaCreditoCompraRepositorio> _notas = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();
    private readonly Mock<IServicioHash> _hash = new();
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
        Usuario("jefa", id: 9, rol: Administrador);
        Usuario("cajero", id: 10, rol: Operador);
        Usuario("yo", id: DoblesBaja.UsuarioEnSesion, rol: Administrador);
        _hash.Setup(h => h.VerificarClave("clave-buena", It.IsAny<string>())).Returns(true);
    }

    private void Usuario(string nombre, int id, int rol) =>
        _usuarios.Setup(u => u.ObtenerPorNombreAsync(nombre, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Id = id, NombreUsuario = nombre, HashContrasena = $"hash-{nombre}", RolId = rol, Activo = true });

    private AnularNotaCreditoCompraUseCase Caso() =>
        new(_notas.Object, _usuarios.Object, _hash.Object, _unidad.Object, _contexto.Object);

    private static AnularNotaCreditoCompraRequest Pedido(string? motivo = " Mal cargada ", string? autoriza = "jefa",
        string? clave = "clave-buena") => new(motivo, autoriza, clave);

    private async Task Rechaza(int notaId, AnularNotaCreditoCompraRequest pedido, TipoError tipo)
    {
        var resultado = await Caso().EjecutarAsync(notaId, pedido);

        Assert.Equal((false, tipo), (resultado.IsSuccess, resultado.Tipo));
        Assert.False(string.IsNullOrWhiteSpace(resultado.Error));
        _notas.Verify(n => n.AnularConEfectosAsync(It.IsAny<AnulacionNotaCreditoCompra>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConAutorizacionValida_AnulaConMotivoSesionYAutorizante()
    {
        var resultado = await Caso().EjecutarAsync(NotaVigente, Pedido());

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.Equal((NotaVigente, "Mal cargada", DoblesBaja.UsuarioEnSesion, 9),
            (_anulacion?.NotaId, _anulacion?.Motivo, _anulacion?.UsuarioId, _anulacion?.UsuarioAutorizacionId));
        Assert.True((DateTime.UtcNow - _anulacion!.Fecha).Duration() < TimeSpan.FromMinutes(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task SinMotivo_Invalido(string? motivo) => await Rechaza(NotaVigente, Pedido(motivo: motivo), TipoError.Invalido);

    [Fact]
    public async Task NotaInexistente_NoEncontrado() => await Rechaza(999, Pedido(), TipoError.NoEncontrado);

    [Fact]
    public async Task NotaYaAnulada_Conflicto() => await Rechaza(NotaAnulada, Pedido(), TipoError.Conflicto);

    [Theory]
    [InlineData("jefa", "clave-mala")]
    [InlineData("cajero", "clave-buena")]
    [InlineData("yo", "clave-buena")]
    public async Task SinDobleCertificacionValida_Invalido(string autoriza, string clave)
        => await Rechaza(NotaVigente, Pedido(autoriza: autoriza, clave: clave), TipoError.Invalido);

    [Fact]
    public async Task SinUsuarioEnLaSesion_NoAutorizado()
    {
        _contexto = DoblesBaja.Contexto(0);

        await Rechaza(NotaVigente, Pedido(), TipoError.NoAutorizado);
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

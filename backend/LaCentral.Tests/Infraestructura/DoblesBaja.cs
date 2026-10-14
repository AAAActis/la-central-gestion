using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Infraestructura;

/// <summary>Dobles comunes a los casos de uso de baja y reactivación (D10).</summary>
public static class DoblesBaja
{
    public const int UsuarioEnSesion = 7;

    public static Mock<IContextoUsuario> Contexto(int usuarioId = UsuarioEnSesion)
    {
        var contexto = new Mock<IContextoUsuario>();
        contexto.SetupGet(c => c.UsuarioId).Returns(usuarioId);
        return contexto;
    }

    /// <summary>Unidad de trabajo que ejecuta la operación tal cual, sin transacción.</summary>
    public static Mock<IUnidadDeTrabajo> UnidadQueEjecuta()
    {
        var unidad = new Mock<IUnidadDeTrabajo>();
        unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<Result>> operacion, CancellationToken ct) => operacion(ct));
        return unidad;
    }

    /// <summary>Unidad de trabajo que NO ejecuta la operación: lo que se escriba igual quedó afuera.</summary>
    public static Mock<IUnidadDeTrabajo> UnidadQueNoEjecuta()
    {
        var unidad = new Mock<IUnidadDeTrabajo>();
        unidad.Setup(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        return unidad;
    }
}

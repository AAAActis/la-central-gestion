using Moq;
using Xunit;
using LaCentral.UseCases;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;

namespace LaCentral.Tests.Acceso;

public class DarDeBajaUsuarioUseCaseTests
{
    private readonly Mock<IUsuarioRepositorio> _repoMock = new();
    private readonly Mock<IContextoUsuario> _contextoMock = new();

    private DarDeBajaUsuarioUseCase CrearCasoDeUso() => 
        new DarDeBajaUsuarioUseCase(_repoMock.Object, _contextoMock.Object);
        

    [Fact]
    public async Task Ejecutar_BajaSinMotivo_DevuelveFalloInvalido()
    {
        // Arrange
        var caso = CrearCasoDeUso();

        // Act
        // El idEnSesion va a ser 0 por defecto en el mock, por lo que el caso de uso
        // debería fallar primero por el motivo antes de chequear la sesión.
        var resultado = await caso.EjecutarAsync(2, ""); 

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("motivo", resultado.Error.ToLower());
    }

    [Fact]
    public async Task Ejecutar_BajaDeSiMismo_DevuelveFalloInvalido()
    {
        // Arrange
        // Simulamos que el operador actual tiene el ID 1
        _contextoMock.Setup(c => c.UsuarioId).Returns(1); 
        var caso = CrearCasoDeUso();

        // Act
        // Intentamos dar de baja al mismo ID 1
        var resultado = await caso.EjecutarAsync(1, "Motivo válido"); 

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("sí mismo", resultado.Error.ToLower());
    }

    [Fact]
    public async Task Ejecutar_BajaUltimoAdministrador_DevuelveConflicto()
    {
        // Arrange
        _contextoMock.Setup(c => c.UsuarioId).Returns(2); // Operador distinto al que se da de baja

        var usuarioABajar = new Usuario { Id = 1, RolId = 2, Activo = true }; // Rol 2 = ADMINISTRADOR
        
        _repoMock.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioABajar);
            
        // Simulamos que al contar en la base, este es el único que queda
        _repoMock.Setup(r => r.ContarAdministradoresActivosAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); 

        var caso = CrearCasoDeUso();

        // Act
        var resultado = await caso.EjecutarAsync(1, "Motivo válido");

        // Assert
        Assert.False(resultado.IsSuccess);
        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("único administrador", resultado.Error.ToLower());
    }

    [Fact]
    public async Task Ejecutar_BajaValida_RegistraMotivoYUsuarioResponsable()
    {
        // Arrange
        int idOperadorEnSesion = 2; // Simulamos que el puesto 2 ejecuta la acción
        _contextoMock.Setup(c => c.UsuarioId).Returns(idOperadorEnSesion); 
        
        var usuarioABajar = new Usuario { Id = 1, RolId = 1, Activo = true };
        
        _repoMock.Setup(r => r.ObtenerPorIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(usuarioABajar);

        // Variable para capturar la entidad que viaja al repositorio
        Usuario? usuarioActualizado = null;
        _repoMock.Setup(r => r.ActualizarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()))
            .Callback<Usuario, CancellationToken>((u, ct) => usuarioActualizado = u)
            .Returns(Task.CompletedTask);
            
        var caso = CrearCasoDeUso();

        // Act
        var resultado = await caso.EjecutarAsync(1, "Cese de actividades");

        // Assert
        Assert.True(resultado.IsSuccess);
        Assert.NotNull(usuarioActualizado);
        Assert.False(usuarioActualizado.Activo);
        // CA5: Guarda el motivo de la anulación/baja
        Assert.Equal("Cese de actividades", usuarioActualizado.MotivoBaja);
        // CA4: Atribuye la operación al puesto que la ejecutó
        Assert.Equal(idOperadorEnSesion, usuarioActualizado.UsuarioBajaId); 
    }
}
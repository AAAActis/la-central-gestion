using Xunit;
using Microsoft.EntityFrameworkCore;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;

namespace LaCentral.Tests.Data;

public class UsuarioRepositorioTests
{
    // =========================================================
    // HALLAZGO 8: FALLA SILENCIOSA EN UPDATE
    // =========================================================
    
    [Fact]
    public async Task ActualizarAsync_UsuarioInexistente_DevuelveFalseNoFallaEnSilencio()
    {
        // Arrange: Levantamos un contexto en memoria aislado
        var options = new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new LaCentralDbContext(options);
        var repo = new UsuarioRepositorio(context);

        // Creamos una entidad de dominio con un ID que sabemos que no existe en la base (999)
        var usuarioInexistente = new LaCentral.UseCases.Entidades.Usuario 
        { 
            Id = 999, 
            NombreUsuario = "fantasma",
            HashContrasena = "xxx",
            Activo = false // Simulamos que le quisimos dar la baja
        };

        // Act
        var resultado = await repo.ActualizarAsync(usuarioInexistente, CancellationToken.None);

        // Assert
        Assert.False(resultado, "El repositorio debería informar que no encontró el registro devolviendo false, no simular un éxito.");
    }
}
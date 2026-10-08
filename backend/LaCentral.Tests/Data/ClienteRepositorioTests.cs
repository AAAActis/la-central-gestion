using Microsoft.EntityFrameworkCore;
using Xunit;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using System;
using System.Threading.Tasks;
using LaCentral.UseCases;

namespace LaCentral.Tests.Data;

public class ClienteRepositorioTests
{
    [Fact]
    public async Task Reactivacion_ConservaMotivoYFechaBaja_ConRepositorioRealInMemory()
    {
        var opciones = new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        var fechaBaja = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        using (var contextoSetup = new LaCentralDbContext(opciones))
        {
            contextoSetup.Clientes.Add(new Cliente
            {
                Id = 1, Codigo = "CLI-01", RazonSocial = "Cliente Test", CuitCuil = "30-11111111-1",
                Activo = false, MotivoBaja = "Cierre temporal", FechaBaja = fechaBaja, FechaAlta = DateTime.UtcNow
            });
            await contextoSetup.SaveChangesAsync();
        }

        using (var contexto = new LaCentralDbContext(opciones))
        {
            var repo = new ClienteRepositorio(contexto);
            var cliente = await repo.ObtenerDetallePorIdAsync(1);
            Assert.NotNull(cliente);
            Assert.False(cliente.Activo);
            Assert.Equal("Cierre temporal", cliente.MotivoBaja);
            Assert.Equal(fechaBaja, cliente.FechaBaja);

            var resultado = await new ReactivarClienteUseCase(repo).EjecutarAsync(1);
            Assert.True(resultado.IsSuccess);
        }

        using (var contextoValidacion = new LaCentralDbContext(opciones))
        {
            var fila = await contextoValidacion.Clientes.FindAsync(1);
            Assert.True(fila!.Activo);
            Assert.Equal("Cierre temporal", fila.MotivoBaja);
            Assert.Equal(fechaBaja, fila.FechaBaja);
        }
    }
}

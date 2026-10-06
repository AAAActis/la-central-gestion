using Microsoft.EntityFrameworkCore;
using Xunit;
using LaCentral.Data.Models;
using LaCentral.Data.Repositorios;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using System;
using System.Threading.Tasks;

namespace LaCentral.Tests.Data;

public class ProveedorRepositorioTests
{
    private DbContextOptions<LaCentralDbContext> ObtenerOpcionesInMemory() =>
        new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

    [Fact]
    public async Task BajaReactivacionYModificacion_MapeaEstadoYMotivoCorrectamente_EnBaseDeDatos()
    {
        var opciones = ObtenerOpcionesInMemory();
        
        using (var contextoSetup = new LaCentralDbContext(opciones))
        {
            var proveedorDb = new Proveedor { Id = 1, RazonSocial = "Prov Test", Codigo = "PRV-01", Activo = true, FechaAlta = DateTime.UtcNow };
            contextoSetup.Proveedors.Add(proveedorDb);
            await contextoSetup.SaveChangesAsync();
        }

        // 1. BAJA (TODO 1)
        using (var contextoBaja = new LaCentralDbContext(opciones))
        {
            var repoBaja = new ProveedorRepositorio(contextoBaja);
            var casoUsoBaja = new DarDeBajaProveedorUseCase(repoBaja);
            await casoUsoBaja.EjecutarAsync(1, new DarDeBajaProveedorRequest("Cierre temporal", "PRV-01"));
        }

        using (var contextoValidacionBaja = new LaCentralDbContext(opciones))
        {
            var filaBaja = await contextoValidacionBaja.Proveedors.FindAsync(1);
            Assert.False(filaBaja!.Activo);
            Assert.Equal("Cierre temporal", filaBaja.MotivoBaja);
            Assert.NotNull(filaBaja.FechaBaja);
        }

        // 2. REACTIVACIÓN (TODO 2)
        using (var contextoReactivacion = new LaCentralDbContext(opciones))
        {
            var repoReactivacion = new ProveedorRepositorio(contextoReactivacion);
            var casoUsoReactivacion = new ReactivarProveedorUseCase(repoReactivacion);
            await casoUsoReactivacion.EjecutarAsync(1);
        }

        using (var contextoValidacionReactivacion = new LaCentralDbContext(opciones))
        {
            var filaReactivada = await contextoValidacionReactivacion.Proveedors.FindAsync(1);
            Assert.True(filaReactivada!.Activo);
            Assert.Equal("Cierre temporal", filaReactivada.MotivoBaja); 
        }

        // 3. MODIFICACIÓN (TODO 3)
        using (var contextoModificacion = new LaCentralDbContext(opciones))
        {
            var repoModificacion = new ProveedorRepositorio(contextoModificacion);
            var proveedorModificado = await repoModificacion.ObtenerDetallePorIdAsync(1);
            proveedorModificado!.RazonSocial = "Nombre Nuevo";
            await repoModificacion.ActualizarAsync(proveedorModificado);
        }

        using (var contextoValidacionModificacion = new LaCentralDbContext(opciones))
        {
            var filaModificada = await contextoValidacionModificacion.Proveedors.FindAsync(1);
            Assert.True(filaModificada!.Activo); 
            Assert.Equal("Nombre Nuevo", filaModificada.RazonSocial);
        }
    }
}
using LaCentral.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Tests.Data;

public class HistorialBajaModeloTests
{
    [Theory]
    [InlineData(typeof(ClienteHistorialBaja), typeof(Cliente), "ClienteId")]
    [InlineData(typeof(ProveedorHistorialBaja), typeof(Proveedor), "ProveedorId")]
    [InlineData(typeof(ArticuloHistorialBaja), typeof(Articulo), "ArticuloId")]
    [InlineData(typeof(UsuarioHistorialBaja), typeof(Usuario), "UsuarioId")]
    public void Historial_MapeaMuchosRegistrosYConservaUnicidadSoloDeLaBajaAbierta(
        Type historial, Type principal, string clave)
    {
        using var context = new LaCentralDbContext(new DbContextOptionsBuilder<LaCentralDbContext>()
            .UseNpgsql("Host=localhost;Database=solo_modelo_test;Username=sin_conexion").Options);
        var entidad = context.Model.FindEntityType(historial)!;
        var relacion = Assert.Single(entidad.GetForeignKeys(), f =>
            f.PrincipalEntityType.ClrType == principal && f.Properties.Any(p => p.Name == clave));
        Assert.False(relacion.IsUnique);
        Assert.True(relacion.PrincipalToDependent!.IsCollection);
        var indice = Assert.Single(entidad.GetIndexes(), i =>
            i.Properties.Count == 1 && i.Properties[0].Name == clave && i.IsUnique);
        Assert.Contains("fecha_reactivacion IS NULL", indice.GetFilter());
        Assert.DoesNotContain(entidad.GetProperties(), p => p.IsShadowProperty());
    }
}

using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Models;

public partial class LaCentralDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        // No editar el scaffold para corregir inferencias de índices filtrados.
        modelBuilder.Entity<Cliente>().Ignore(e => e.ClienteHistorialBaja);
        modelBuilder.Entity<ClienteHistorialBaja>().HasOne(e => e.Cliente)
            .WithMany(e => e.HistorialesBaja).HasForeignKey(e => e.ClienteId)
            .HasConstraintName("cliente_historial_baja_cliente_id_fkey");

        modelBuilder.Entity<Proveedor>().Ignore(e => e.ProveedorHistorialBaja);
        modelBuilder.Entity<ProveedorHistorialBaja>().HasOne(e => e.Proveedor)
            .WithMany(e => e.HistorialesBaja).HasForeignKey(e => e.ProveedorId)
            .HasConstraintName("proveedor_historial_baja_proveedor_id_fkey");

        modelBuilder.Entity<Articulo>().Ignore(e => e.ArticuloHistorialBaja);
        modelBuilder.Entity<ArticuloHistorialBaja>().HasOne(e => e.Articulo)
            .WithMany(e => e.HistorialesBaja).HasForeignKey(e => e.ArticuloId)
            .HasConstraintName("articulo_historial_baja_articulo_id_fkey");

        modelBuilder.Entity<Usuario>().Ignore(e => e.UsuarioHistorialBajaUsuario);
        modelBuilder.Entity<UsuarioHistorialBaja>().HasOne(e => e.Usuario)
            .WithMany(e => e.HistorialesBaja).HasForeignKey(e => e.UsuarioId)
            .HasConstraintName("usuario_historial_baja_usuario_id_fkey");
    }
}

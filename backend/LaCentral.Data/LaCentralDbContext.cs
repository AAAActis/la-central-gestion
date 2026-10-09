using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Data.Models;

public partial class LaCentralDbContext : DbContext
{
    public LaCentralDbContext(DbContextOptions<LaCentralDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Articulo> Articulo { get; set; }

    public virtual DbSet<ArticuloCodigoAlternativo> ArticuloCodigoAlternativo { get; set; }

    public virtual DbSet<ArticuloHistorialBaja> ArticuloHistorialBaja { get; set; }

    public virtual DbSet<ArticuloHistorialCompra> ArticuloHistorialCompra { get; set; }

    public virtual DbSet<ArticuloMargen> ArticuloMargen { get; set; }

    public virtual DbSet<Cliente> Cliente { get; set; }

    public virtual DbSet<ClienteDireccion> ClienteDireccion { get; set; }

    public virtual DbSet<ClienteHistorialBaja> ClienteHistorialBaja { get; set; }


    public virtual DbSet<ClienteTelefono> ClienteTelefono { get; set; }

    public virtual DbSet<FacturaCompra> FacturaCompra { get; set; }

    public virtual DbSet<FacturaCompraDetalle> FacturaCompraDetalle { get; set; }

    public virtual DbSet<FacturaVenta> FacturaVenta { get; set; }

    public virtual DbSet<FacturaVentaDetalle> FacturaVentaDetalle { get; set; }

    public virtual DbSet<PrecioProveedor> PrecioProveedor { get; set; }

    public virtual DbSet<Proveedor> Proveedor { get; set; }

    public virtual DbSet<ProveedorCredencial> ProveedorCredencial { get; set; }

    public virtual DbSet<ProveedorDireccion> ProveedorDireccion { get; set; }

    public virtual DbSet<ProveedorHistorialBaja> ProveedorHistorialBaja { get; set; }

    public virtual DbSet<ProveedorTelefono> ProveedorTelefono { get; set; }

    public virtual DbSet<Rol> Rol { get; set; }

    public virtual DbSet<Stock> Stock { get; set; }

    public virtual DbSet<Sucursal> Sucursal { get; set; }

    public virtual DbSet<Transferencia> Transferencia { get; set; }

    public virtual DbSet<TransferenciaDetalle> TransferenciaDetalle { get; set; }

    public virtual DbSet<Usuario> Usuario { get; set; }

    public virtual DbSet<UsuarioHistorialBaja> UsuarioHistorialBaja { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");

        modelBuilder.Entity<Articulo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("articulo_pkey");

            entity.ToTable("articulo");

            entity.HasIndex(e => e.CodigoInterno, "articulo_codigo_interno_key").IsUnique();

            entity.HasIndex(e => e.Nombre, "ix_articulo_nombre").UseCollation(new[] { "es-AR-x-icu" });

            entity.HasIndex(e => e.Nombre, "ix_articulo_nombre_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" })
                .UseCollation(new[] { "es-AR-x-icu" });

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.CodigoInterno)
                .HasMaxLength(40)
                .HasColumnName("codigo_interno");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.MotivoBaja).HasColumnName("motivo_baja");
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("nombre");
            entity.Property(e => e.PrecioCosto)
                .HasPrecision(14, 2)
                .HasComment("DENORMALIZACIÓN DELIBERADA: valor derivado del último registro válido de articulo_historial_compra. Se mantiene acá para no recalcularlo en cada consulta del comparador.")
                .HasColumnName("precio_costo");
            entity.Property(e => e.PrecioVentaEstimado)
                .HasPrecision(14, 2)
                .HasColumnName("precio_venta_estimado");
            entity.Property(e => e.UbicacionDeposito)
                .HasMaxLength(60)
                .HasComment("Posición física de referencia en el depósito, en texto libre. Dato informativo: el sistema no gestiona el depósito.")
                .HasColumnName("ubicacion_deposito");
            entity.Property(e => e.UltimoProveedorId).HasColumnName("ultimo_proveedor_id");

            entity.HasOne(d => d.UltimoProveedor).WithMany(p => p.Articulo)
                .HasForeignKey(d => d.UltimoProveedorId)
                .HasConstraintName("articulo_ultimo_proveedor_id_fkey");
        });

        modelBuilder.Entity<ArticuloCodigoAlternativo>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("articulo_codigo_alternativo_pkey");

            entity.ToTable("articulo_codigo_alternativo");

            entity.HasIndex(e => e.Codigo, "codigo_unico_global").IsUnique();

            entity.HasIndex(e => e.ArticuloId, "ix_codigo_alt_articulo");

            entity.HasIndex(e => e.ProveedorId, "ix_codigo_alt_proveedor");

            entity.HasIndex(e => new { e.ArticuloId, e.ProveedorId }, "un_codigo_por_proveedor").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.Codigo)
                .HasMaxLength(60)
                .HasColumnName("codigo");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");

            entity.HasOne(d => d.Articulo).WithMany(p => p.ArticuloCodigoAlternativo)
                .HasForeignKey(d => d.ArticuloId)
                .HasConstraintName("articulo_codigo_alternativo_articulo_id_fkey");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ArticuloCodigoAlternativo)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("articulo_codigo_alternativo_proveedor_id_fkey");
        });

        modelBuilder.Entity<ArticuloHistorialBaja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("articulo_historial_baja_pkey");

            entity.ToTable("articulo_historial_baja");

            entity.HasIndex(e => new { e.ArticuloId, e.FechaBaja }, "ix_articulo_hist_baja").IsDescending(false, true);

            entity.HasIndex(e => e.ArticuloId, "ux_articulo_hist_abierta")
                .IsUnique()
                .HasFilter("(fecha_reactivacion IS NULL)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.FechaReactivacion).HasColumnName("fecha_reactivacion");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");
            entity.Property(e => e.UsuarioReactivacionId).HasColumnName("usuario_reactivacion_id");

            entity.HasOne(d => d.Articulo).WithOne(p => p.ArticuloHistorialBaja)
                .HasForeignKey<ArticuloHistorialBaja>(d => d.ArticuloId)
                .HasConstraintName("articulo_historial_baja_articulo_id_fkey");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.ArticuloHistorialBajaUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("articulo_historial_baja_usuario_baja_id_fkey");

            entity.HasOne(d => d.UsuarioReactivacion).WithMany(p => p.ArticuloHistorialBajaUsuarioReactivacion)
                .HasForeignKey(d => d.UsuarioReactivacionId)
                .HasConstraintName("articulo_historial_baja_usuario_reactivacion_id_fkey");
        });

        modelBuilder.Entity<ArticuloHistorialCompra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("articulo_historial_compra_pkey");

            entity.ToTable("articulo_historial_compra");

            entity.HasIndex(e => new { e.ArticuloId, e.Fecha }, "ix_historial_articulo")
                .IsDescending(false, true)
                .HasFilter("(NOT anulado)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Anulado).HasColumnName("anulado");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.FacturaCompraId).HasColumnName("factura_compra_id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.PrecioCosto)
                .HasPrecision(14, 2)
                .HasColumnName("precio_costo");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");

            entity.HasOne(d => d.Articulo).WithMany(p => p.ArticuloHistorialCompra)
                .HasForeignKey(d => d.ArticuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("articulo_historial_compra_articulo_id_fkey");

            entity.HasOne(d => d.FacturaCompra).WithMany(p => p.ArticuloHistorialCompra)
                .HasForeignKey(d => d.FacturaCompraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_historial_factura");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ArticuloHistorialCompra)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("articulo_historial_compra_proveedor_id_fkey");
        });

        modelBuilder.Entity<ArticuloMargen>(entity =>
        {
            entity.HasKey(e => new { e.ArticuloId, e.Numero }).HasName("articulo_margen_pkey");

            entity.ToTable("articulo_margen", tb => tb.HasComment("En La Central se usan tres márgenes: 50 %, 40 % y 0,01 %. El último representa precio de venta igual al costo, porque el sistema anterior no admite cargar cero."));

            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.Numero).HasColumnName("numero");
            entity.Property(e => e.Porcentaje)
                .HasPrecision(6, 2)
                .HasColumnName("porcentaje");

            entity.HasOne(d => d.Articulo).WithMany(p => p.ArticuloMargen)
                .HasForeignKey(d => d.ArticuloId)
                .HasConstraintName("articulo_margen_articulo_id_fkey");
        });


        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_pkey");

            entity.ToTable("cliente");

            entity.HasIndex(e => e.Codigo, "cliente_codigo_key").IsUnique();

            entity.HasIndex(e => e.CuitCuil, "cliente_cuit_cuil_key").IsUnique();

            entity.HasIndex(e => e.RazonSocial, "ix_cliente_razon_social").UseCollation(new[] { "es-AR-x-icu" });

            entity.HasIndex(e => e.RazonSocial, "ix_cliente_razon_social_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" })
                .UseCollation(new[] { "es-AR-x-icu" });

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasComment("Clave de negocio, heredada de Multisoft. Reemplaza al CUIT como identificador único porque el sistema de origen no lo tiene cargado en la mayoría de los registros.")
                .HasColumnName("codigo");
            entity.Property(e => e.CondicionFiscal)
                .HasMaxLength(30)
                .HasColumnName("condicion_fiscal");
            entity.Property(e => e.CondicionPago)
                .HasMaxLength(60)
                .HasColumnName("condicion_pago");
            entity.Property(e => e.CuitCuil)
                .HasMaxLength(13)
                .HasColumnName("cuit_cuil");
            entity.Property(e => e.CuitSimulado)
                .HasComment("Verdadero cuando el CUIT fue generado por el equipo. Los simulados usan un bloque de documento que arranca en 90.000.000, inexistente entre los documentos reales.")
                .HasColumnName("cuit_simulado");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.MotivoBaja).HasColumnName("motivo_baja");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(120)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("razon_social");
            entity.Property(e => e.TieneCuentaCorriente)
                .HasComment("Dato informativo. El sistema NO lleva saldo ni deuda: Cuentas a Cobrar está fuera del alcance, en coherencia con la exclusión de Cuentas a Pagar.")
                .HasColumnName("tiene_cuenta_corriente");
            entity.Property(e => e.UsuarioAltaId).HasColumnName("usuario_alta_id");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");

            entity.HasOne(d => d.UsuarioAlta).WithMany(p => p.ClienteUsuarioAlta)
                .HasForeignKey(d => d.UsuarioAltaId)
                .HasConstraintName("cliente_usuario_alta_id_fkey");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.ClienteUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("cliente_usuario_baja_id_fkey");
        });

        modelBuilder.Entity<ClienteDireccion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_direccion_pkey");

            entity.ToTable("cliente_direccion");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Barrio)
                .HasMaxLength(80)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("barrio");
            entity.Property(e => e.Calle)
                .HasMaxLength(120)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("calle");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.CodigoPostal)
                .HasMaxLength(10)
                .HasColumnName("codigo_postal");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(40)
                .HasColumnName("descripcion");
            entity.Property(e => e.Localidad)
                .HasMaxLength(80)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("localidad");
            entity.Property(e => e.Provincia)
                .HasMaxLength(60)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("provincia");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ClienteDireccion)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("cliente_direccion_cliente_id_fkey");
        });

        modelBuilder.Entity<ClienteHistorialBaja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_historial_baja_pkey");

            entity.ToTable("cliente_historial_baja", tb => tb.HasComment("Una fila por cada baja. fecha_reactivacion NULL = baja vigente. El índice único parcial impide dos bajas abiertas simultáneas del mismo cliente."));

            entity.HasIndex(e => new { e.ClienteId, e.FechaBaja }, "ix_cliente_hist_baja").IsDescending(false, true);

            entity.HasIndex(e => e.ClienteId, "ux_cliente_hist_abierta")
                .IsUnique()
                .HasFilter("(fecha_reactivacion IS NULL)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.FechaReactivacion).HasColumnName("fecha_reactivacion");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");
            entity.Property(e => e.UsuarioReactivacionId).HasColumnName("usuario_reactivacion_id");

            entity.HasOne(d => d.Cliente).WithOne(p => p.ClienteHistorialBaja)
                .HasForeignKey<ClienteHistorialBaja>(d => d.ClienteId)
                .HasConstraintName("cliente_historial_baja_cliente_id_fkey");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.ClienteHistorialBajaUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("cliente_historial_baja_usuario_baja_id_fkey");

            entity.HasOne(d => d.UsuarioReactivacion).WithMany(p => p.ClienteHistorialBajaUsuarioReactivacion)
                .HasForeignKey(d => d.UsuarioReactivacionId)
                .HasConstraintName("cliente_historial_baja_usuario_reactivacion_id_fkey");
        });

        modelBuilder.Entity<ClienteTelefono>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cliente_telefono_pkey");

            entity.ToTable("cliente_telefono");

            entity.HasIndex(e => new { e.ClienteId, e.Numero }, "cliente_telefono_cliente_id_numero_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(60)
                .HasColumnName("descripcion");
            entity.Property(e => e.Numero)
                .HasMaxLength(60)
                .HasColumnName("numero");

            entity.HasOne(d => d.Cliente).WithMany(p => p.ClienteTelefono)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("cliente_telefono_cliente_id_fkey");
        });

        modelBuilder.Entity<FacturaCompra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("factura_compra_pkey");

            entity.ToTable("factura_compra", tb => tb.HasComment("Factura de compra de un proveedor. Importes en pesos argentinos: la empresa no opera en moneda extranjera (decisión D7, 07/10/2026). El sistema anterior admite moneda y cotización por comprobante y por línea (APS-005); acá queda excluido del alcance de forma deliberada. El destino de stock se carga por línea, no por comprobante (D9)."));

            entity.HasIndex(e => new { e.ProveedorId, e.Numero }, "ux_factura_compra_numero_activa")
                .IsUnique()
                .HasFilter("((estado)::text = 'ACTIVA'::text)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .HasDefaultValueSql("'ACTIVA'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.FechaAnulacion).HasColumnName("fecha_anulacion");
            entity.Property(e => e.FechaEmision).HasColumnName("fecha_emision");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(e => e.MotivoAnulacion).HasColumnName("motivo_anulacion");
            entity.Property(e => e.Numero)
                .HasMaxLength(30)
                .HasColumnName("numero");
            entity.Property(e => e.Origen)
                .HasMaxLength(10)
                .HasDefaultValueSql("'MANUAL'::character varying")
                .HasColumnName("origen");
            entity.Property(e => e.PercepcionGanancias)
                .HasPrecision(14, 2)
                .HasDefaultValue(0m)
                .HasComment("Percepción de Ganancias. Dato transcripto.")
                .HasColumnName("percepcion_ganancias");
            entity.Property(e => e.PercepcionIibb)
                .HasPrecision(14, 2)
                .HasDefaultValue(0m)
                .HasComment("Percepción de Ingresos Brutos. Dato transcripto, sin cálculo por jurisdicción.")
                .HasColumnName("percepcion_iibb");
            entity.Property(e => e.PercepcionIva)
                .HasPrecision(14, 2)
                .HasDefaultValue(0m)
                .HasComment("Percepción de IVA que el proveedor adiciona al comprobante. Dato transcripto del PDF: el sistema no calcula alícuotas ni liquida impuestos.")
                .HasColumnName("percepcion_iva");
            entity.Property(e => e.PercepcionMunicipal)
                .HasPrecision(14, 2)
                .HasDefaultValue(0m)
                .HasComment("Percepción municipal. Dato transcripto.")
                .HasColumnName("percepcion_municipal");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.UsuarioAnulacionId).HasColumnName("usuario_anulacion_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.FacturaCompra)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_compra_proveedor_id_fkey");

            entity.HasOne(d => d.UsuarioAnulacion).WithMany(p => p.FacturaCompraUsuarioAnulacion)
                .HasForeignKey(d => d.UsuarioAnulacionId)
                .HasConstraintName("factura_compra_usuario_anulacion_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.FacturaCompraUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_compra_usuario_id_fkey");
        });

        modelBuilder.Entity<FacturaCompraDetalle>(entity =>
        {
            entity.HasKey(e => new { e.FacturaCompraId, e.ArticuloId }).HasName("factura_compra_detalle_pkey");

            entity.ToTable("factura_compra_detalle");

            entity.HasIndex(e => e.SucursalId, "ix_fc_detalle_sucursal");

            entity.Property(e => e.FacturaCompraId).HasColumnName("factura_compra_id");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(12, 3)
                .HasColumnName("cantidad");
            entity.Property(e => e.DescuentoPorcentaje)
                .HasPrecision(6, 2)
                .HasColumnName("descuento_porcentaje");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(14, 2)
                .HasColumnName("precio_unitario");
            entity.Property(e => e.SucursalId)
                .HasComment("Destino de la mercadería de ESTA línea. Una factura puede repartirse entre Fragueiro y San Vicente, y el sistema anterior lo carga por línea (APS-005, columna «Dep.»). El stock se incrementa contra esta sucursal, no contra una del comprobante.")
                .HasColumnName("sucursal_id");

            entity.HasOne(d => d.Articulo).WithMany(p => p.FacturaCompraDetalle)
                .HasForeignKey(d => d.ArticuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_compra_detalle_articulo_id_fkey");

            entity.HasOne(d => d.FacturaCompra).WithMany(p => p.FacturaCompraDetalle)
                .HasForeignKey(d => d.FacturaCompraId)
                .HasConstraintName("factura_compra_detalle_factura_compra_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.FacturaCompraDetalle)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_compra_detalle_sucursal_id_fkey");
        });

        modelBuilder.Entity<FacturaVenta>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("factura_venta_pkey");

            entity.ToTable("factura_venta");

            entity.HasIndex(e => e.Numero, "ux_factura_venta_numero_activa")
                .IsUnique()
                .HasFilter("((estado)::text = 'ACTIVA'::text)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.ClienteId).HasColumnName("cliente_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(10)
                .HasDefaultValueSql("'ACTIVA'::character varying")
                .HasColumnName("estado");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.FechaAnulacion).HasColumnName("fecha_anulacion");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.MotivoAnulacion).HasColumnName("motivo_anulacion");
            entity.Property(e => e.Movil)
                .HasMaxLength(40)
                .HasColumnName("movil");
            entity.Property(e => e.Numero)
                .HasMaxLength(30)
                .HasColumnName("numero");
            entity.Property(e => e.OrdenCompra)
                .HasMaxLength(40)
                .HasColumnName("orden_compra");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.UsuarioAnulacionId).HasColumnName("usuario_anulacion_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Cliente).WithMany(p => p.FacturaVenta)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_venta_cliente_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.FacturaVenta)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_venta_sucursal_id_fkey");

            entity.HasOne(d => d.UsuarioAnulacion).WithMany(p => p.FacturaVentaUsuarioAnulacion)
                .HasForeignKey(d => d.UsuarioAnulacionId)
                .HasConstraintName("factura_venta_usuario_anulacion_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.FacturaVentaUsuario)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_venta_usuario_id_fkey");
        });

        modelBuilder.Entity<FacturaVentaDetalle>(entity =>
        {
            entity.HasKey(e => new { e.FacturaVentaId, e.ArticuloId }).HasName("factura_venta_detalle_pkey");

            entity.ToTable("factura_venta_detalle");

            entity.Property(e => e.FacturaVentaId).HasColumnName("factura_venta_id");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(12, 3)
                .HasColumnName("cantidad");
            entity.Property(e => e.PrecioUnitario)
                .HasPrecision(14, 2)
                .HasColumnName("precio_unitario");

            entity.HasOne(d => d.Articulo).WithMany(p => p.FacturaVentaDetalle)
                .HasForeignKey(d => d.ArticuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("factura_venta_detalle_articulo_id_fkey");

            entity.HasOne(d => d.FacturaVenta).WithMany(p => p.FacturaVentaDetalle)
                .HasForeignKey(d => d.FacturaVentaId)
                .HasConstraintName("factura_venta_detalle_factura_venta_id_fkey");
        });

        modelBuilder.Entity<PrecioProveedor>(entity =>
        {
            entity.HasKey(e => new { e.ArticuloId, e.ProveedorId }).HasName("precio_proveedor_pkey");

            entity.ToTable("precio_proveedor");

            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.FechaObtencion).HasColumnName("fecha_obtencion");
            entity.Property(e => e.Origen)
                .HasMaxLength(20)
                .HasColumnName("origen");
            entity.Property(e => e.PrecioBonificado)
                .HasPrecision(14, 2)
                .HasColumnName("precio_bonificado");
            entity.Property(e => e.PrecioLista)
                .HasPrecision(14, 2)
                .HasColumnName("precio_lista");

            entity.HasOne(d => d.Articulo).WithMany(p => p.PrecioProveedor)
                .HasForeignKey(d => d.ArticuloId)
                .HasConstraintName("precio_proveedor_articulo_id_fkey");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.PrecioProveedor)
                .HasForeignKey(d => d.ProveedorId)
                .HasConstraintName("precio_proveedor_proveedor_id_fkey");
        });

        modelBuilder.Entity<Proveedor>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_pkey");

            entity.ToTable("proveedor");

            entity.HasIndex(e => e.RazonSocial, "ix_proveedor_razon_social").UseCollation(new[] { "es-AR-x-icu" });

            entity.HasIndex(e => e.RazonSocial, "ix_proveedor_razon_social_trgm")
                .HasMethod("gin")
                .HasOperators(new[] { "gin_trgm_ops" })
                .UseCollation(new[] { "es-AR-x-icu" });

            entity.HasIndex(e => e.Codigo, "proveedor_codigo_key").IsUnique();

            entity.HasIndex(e => e.Cuit, "proveedor_cuit_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.CondicionFiscal)
                .HasMaxLength(30)
                .HasColumnName("condicion_fiscal");
            entity.Property(e => e.Cuit)
                .HasMaxLength(13)
                .HasColumnName("cuit");
            entity.Property(e => e.CuitSimulado).HasColumnName("cuit_simulado");
            entity.Property(e => e.Email)
                .HasMaxLength(120)
                .HasColumnName("email");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.MotivoBaja).HasColumnName("motivo_baja");
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(120)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("razon_social");
            entity.Property(e => e.UrlReferencia)
                .HasMaxLength(300)
                .HasComment("Página de precios del proveedor, usada por el comparador (HU-CMP-05). Ningún proveedor la tiene cargada en el sistema de origen: hay que relevarla.")
                .HasColumnName("url_referencia");
        });

        modelBuilder.Entity<ProveedorCredencial>(entity =>
        {
            entity.HasKey(e => e.ProveedorId).HasName("proveedor_credencial_pkey");

            entity.ToTable("proveedor_credencial");

            entity.Property(e => e.ProveedorId)
                .ValueGeneratedNever()
                .HasColumnName("proveedor_id");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.SecretoCifrado)
                .HasComment("Contraseña cifrada por la aplicación. La base nunca almacena el valor en claro.")
                .HasColumnName("secreto_cifrado");
            entity.Property(e => e.Usuario)
                .HasMaxLength(120)
                .HasColumnName("usuario");
            entity.Property(e => e.Vigente)
                .HasDefaultValue(true)
                .HasColumnName("vigente");

            entity.HasOne(d => d.Proveedor).WithOne(p => p.ProveedorCredencial)
                .HasForeignKey<ProveedorCredencial>(d => d.ProveedorId)
                .HasConstraintName("proveedor_credencial_proveedor_id_fkey");
        });

        modelBuilder.Entity<ProveedorDireccion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_direccion_pkey");

            entity.ToTable("proveedor_direccion");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Barrio)
                .HasMaxLength(80)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("barrio");
            entity.Property(e => e.Calle)
                .HasMaxLength(120)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("calle");
            entity.Property(e => e.CodigoPostal)
                .HasMaxLength(10)
                .HasColumnName("codigo_postal");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(40)
                .HasColumnName("descripcion");
            entity.Property(e => e.Localidad)
                .HasMaxLength(80)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("localidad");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.Provincia)
                .HasMaxLength(60)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("provincia");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ProveedorDireccion)
                .HasForeignKey(d => d.ProveedorId)
                .HasConstraintName("proveedor_direccion_proveedor_id_fkey");
        });

        modelBuilder.Entity<ProveedorHistorialBaja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_historial_baja_pkey");

            entity.ToTable("proveedor_historial_baja");

            entity.HasIndex(e => new { e.ProveedorId, e.FechaBaja }, "ix_proveedor_hist_baja").IsDescending(false, true);

            entity.HasIndex(e => e.ProveedorId, "ux_proveedor_hist_abierta")
                .IsUnique()
                .HasFilter("(fecha_reactivacion IS NULL)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.FechaReactivacion).HasColumnName("fecha_reactivacion");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");
            entity.Property(e => e.UsuarioReactivacionId).HasColumnName("usuario_reactivacion_id");

            entity.HasOne(d => d.Proveedor).WithOne(p => p.ProveedorHistorialBaja)
                .HasForeignKey<ProveedorHistorialBaja>(d => d.ProveedorId)
                .HasConstraintName("proveedor_historial_baja_proveedor_id_fkey");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.ProveedorHistorialBajaUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("proveedor_historial_baja_usuario_baja_id_fkey");

            entity.HasOne(d => d.UsuarioReactivacion).WithMany(p => p.ProveedorHistorialBajaUsuarioReactivacion)
                .HasForeignKey(d => d.UsuarioReactivacionId)
                .HasConstraintName("proveedor_historial_baja_usuario_reactivacion_id_fkey");
        });

        modelBuilder.Entity<ProveedorTelefono>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("proveedor_telefono_pkey");

            entity.ToTable("proveedor_telefono");

            entity.HasIndex(e => new { e.ProveedorId, e.Numero }, "proveedor_telefono_proveedor_id_numero_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(60)
                .HasColumnName("descripcion");
            entity.Property(e => e.Numero)
                .HasMaxLength(60)
                .HasColumnName("numero");
            entity.Property(e => e.ProveedorId).HasColumnName("proveedor_id");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ProveedorTelefono)
                .HasForeignKey(d => d.ProveedorId)
                .HasConstraintName("proveedor_telefono_proveedor_id_fkey");
        });

        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("rol_pkey");

            entity.ToTable("rol");

            entity.HasIndex(e => e.Nombre, "rol_nombre_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(20)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => new { e.ArticuloId, e.SucursalId }).HasName("stock_pkey");

            entity.ToTable("stock", tb => tb.HasComment("Existencia por artículo y ubicación. Admite valores negativos: la venta sin stock no se bloquea (HU-STK-03)."));

            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(12, 3)
                .HasColumnName("cantidad");

            entity.HasOne(d => d.Articulo).WithMany(p => p.Stock)
                .HasForeignKey(d => d.ArticuloId)
                .HasConstraintName("stock_articulo_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Stock)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("stock_sucursal_id_fkey");
        });

        modelBuilder.Entity<Sucursal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("sucursal_pkey");

            entity.ToTable("sucursal");

            entity.HasIndex(e => e.Codigo, "sucursal_codigo_key").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Codigo)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("codigo");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .UseCollation("es-AR-x-icu")
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<Transferencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transferencia_pkey");

            entity.ToTable("transferencia");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha");
            entity.Property(e => e.Observaciones).HasColumnName("observaciones");
            entity.Property(e => e.SucursalDestinoId).HasColumnName("sucursal_destino_id");
            entity.Property(e => e.SucursalOrigenId).HasColumnName("sucursal_origen_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.SucursalDestino).WithMany(p => p.TransferenciaSucursalDestino)
                .HasForeignKey(d => d.SucursalDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transferencia_sucursal_destino_id_fkey");

            entity.HasOne(d => d.SucursalOrigen).WithMany(p => p.TransferenciaSucursalOrigen)
                .HasForeignKey(d => d.SucursalOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transferencia_sucursal_origen_id_fkey");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Transferencia)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transferencia_usuario_id_fkey");
        });

        modelBuilder.Entity<TransferenciaDetalle>(entity =>
        {
            entity.HasKey(e => new { e.TransferenciaId, e.ArticuloId }).HasName("transferencia_detalle_pkey");

            entity.ToTable("transferencia_detalle");

            entity.Property(e => e.TransferenciaId).HasColumnName("transferencia_id");
            entity.Property(e => e.ArticuloId).HasColumnName("articulo_id");
            entity.Property(e => e.Cantidad)
                .HasPrecision(12, 3)
                .HasColumnName("cantidad");

            entity.HasOne(d => d.Articulo).WithMany(p => p.TransferenciaDetalle)
                .HasForeignKey(d => d.ArticuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("transferencia_detalle_articulo_id_fkey");

            entity.HasOne(d => d.Transferencia).WithMany(p => p.TransferenciaDetalle)
                .HasForeignKey(d => d.TransferenciaId)
                .HasConstraintName("transferencia_detalle_transferencia_id_fkey");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_pkey");

            entity.ToTable("usuario", tb => tb.HasComment("Un usuario representa un puesto de trabajo, no una persona. La trazabilidad alcanza a la terminal desde la que se operó."));

            entity.HasIndex(e => e.NombreUsuario, "usuario_nombre_usuario_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.FechaAlta)
                .HasDefaultValueSql("now()")
                .HasColumnName("fecha_alta");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.HashContrasena)
                .HasMaxLength(255)
                .HasColumnName("hash_contrasena");
            entity.Property(e => e.MotivoBaja).HasColumnName("motivo_baja");
            entity.Property(e => e.NombreUsuario)
                .HasMaxLength(30)
                .HasColumnName("nombre_usuario");
            entity.Property(e => e.RolId).HasColumnName("rol_id");
            entity.Property(e => e.SucursalId).HasColumnName("sucursal_id");
            entity.Property(e => e.UsuarioAltaId).HasColumnName("usuario_alta_id");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");

            entity.HasOne(d => d.Rol).WithMany(p => p.Usuario)
                .HasForeignKey(d => d.RolId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuario_rol_id_fkey");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Usuario)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("usuario_sucursal_id_fkey");

            entity.HasOne(d => d.UsuarioAlta).WithMany(p => p.InverseUsuarioAlta)
                .HasForeignKey(d => d.UsuarioAltaId)
                .HasConstraintName("usuario_usuario_alta_id_fkey");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.InverseUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("usuario_usuario_baja_id_fkey");
        });

        modelBuilder.Entity<UsuarioHistorialBaja>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuario_historial_baja_pkey");

            entity.ToTable("usuario_historial_baja");

            entity.HasIndex(e => new { e.UsuarioId, e.FechaBaja }, "ix_usuario_hist_baja").IsDescending(false, true);

            entity.HasIndex(e => e.UsuarioId, "ux_usuario_hist_abierta")
                .IsUnique()
                .HasFilter("(fecha_reactivacion IS NULL)");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.FechaBaja).HasColumnName("fecha_baja");
            entity.Property(e => e.FechaReactivacion).HasColumnName("fecha_reactivacion");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.UsuarioBajaId).HasColumnName("usuario_baja_id");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");
            entity.Property(e => e.UsuarioReactivacionId).HasColumnName("usuario_reactivacion_id");

            entity.HasOne(d => d.UsuarioBaja).WithMany(p => p.UsuarioHistorialBajaUsuarioBaja)
                .HasForeignKey(d => d.UsuarioBajaId)
                .HasConstraintName("usuario_historial_baja_usuario_baja_id_fkey");

            entity.HasOne(d => d.Usuario).WithOne(p => p.UsuarioHistorialBajaUsuario)
                .HasForeignKey<UsuarioHistorialBaja>(d => d.UsuarioId)
                .HasConstraintName("usuario_historial_baja_usuario_id_fkey");

            entity.HasOne(d => d.UsuarioReactivacion).WithMany(p => p.UsuarioHistorialBajaUsuarioReactivacion)
                .HasForeignKey(d => d.UsuarioReactivacionId)
                .HasConstraintName("usuario_historial_baja_usuario_reactivacion_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

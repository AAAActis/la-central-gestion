using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class Cliente
{
    public int Id { get; set; }

    /// <summary>
    /// Clave de negocio, heredada de Multisoft. Reemplaza al CUIT como identificador único porque el sistema de origen no lo tiene cargado en la mayoría de los registros.
    /// </summary>
    public string Codigo { get; set; } = null!;

    public string RazonSocial { get; set; } = null!;

    public string? CuitCuil { get; set; }

    /// <summary>
    /// Verdadero cuando el CUIT fue generado por el equipo. Los simulados usan un bloque de documento que arranca en 90.000.000, inexistente entre los documentos reales.
    /// </summary>
    public bool CuitSimulado { get; set; }

    public string? CondicionFiscal { get; set; }

    public string? Email { get; set; }

    public string? CondicionPago { get; set; }

    /// <summary>
    /// Dato informativo. El sistema NO lleva saldo ni deuda: Cuentas a Cobrar está fuera del alcance, en coherencia con la exclusión de Cuentas a Pagar.
    /// </summary>
    public bool TieneCuentaCorriente { get; set; }

    public bool Activo { get; set; }

    public string? MotivoBaja { get; set; }

    public DateTime? FechaBaja { get; set; }

    public DateTime FechaAlta { get; set; }

    public int? UsuarioAltaId { get; set; }

    public int? UsuarioBajaId { get; set; }

    public virtual ICollection<ClienteDireccion> ClienteDireccion { get; set; } = new List<ClienteDireccion>();

    public virtual ClienteHistorialBaja? ClienteHistorialBaja { get; set; }

    public virtual ICollection<ClienteTelefono> ClienteTelefono { get; set; } = new List<ClienteTelefono>();

    public virtual ICollection<FacturaVenta> FacturaVenta { get; set; } = new List<FacturaVenta>();

    public virtual Usuario? UsuarioAlta { get; set; }

    public virtual Usuario? UsuarioBaja { get; set; }
}

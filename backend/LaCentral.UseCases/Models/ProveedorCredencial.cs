using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class ProveedorCredencial
{
    public int ProveedorId { get; set; }

    public string Usuario { get; set; } = null!;

    /// <summary>
    /// Contraseña cifrada por la aplicación. La base nunca almacena el valor en claro.
    /// </summary>
    public byte[] SecretoCifrado { get; set; } = null!;

    public bool Vigente { get; set; }

    public DateTime FechaAlta { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;
}

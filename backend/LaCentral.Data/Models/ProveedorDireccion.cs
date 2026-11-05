using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ProveedorDireccion
{
    public int Id { get; set; }

    public int ProveedorId { get; set; }

    public string? Calle { get; set; }

    public string? Barrio { get; set; }

    public string? Localidad { get; set; }

    public string? CodigoPostal { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>
    /// Reemplaza la columna de texto libre `provincia`. NULL significa provincia no registrada. Hallazgo N-5.
    /// </summary>
    public int? ProvinciaId { get; set; }

    public virtual Proveedor Proveedor { get; set; } = null!;

    public virtual Provincia? Provincia { get; set; }
}

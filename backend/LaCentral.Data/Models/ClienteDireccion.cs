using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

public partial class ClienteDireccion
{
    public int Id { get; set; }

    public int ClienteId { get; set; }

    public string? Calle { get; set; }

    public string? Barrio { get; set; }

    /// <summary>
    /// Texto libre, y es una decisión. El nombre de localidad NO determina la provincia: hay homónimos entre provincias (6 casos medidos en el origen), así que no hay dependencia transitiva que eliminar. Normalizarla exigiría el par (nombre, provincia) como clave, que es dato que el origen no trae.
    /// </summary>
    public string? Localidad { get; set; }

    public string? CodigoPostal { get; set; }

    public string? Descripcion { get; set; }

    /// <summary>
    /// Reemplaza la columna de texto libre `provincia`. NULL significa provincia no registrada, que es lo que el origen expresaba con el centinela de código 0. Hallazgo N-5.
    /// </summary>
    public int? ProvinciaId { get; set; }

    public virtual Cliente Cliente { get; set; } = null!;

    public virtual Provincia? Provincia { get; set; }
}

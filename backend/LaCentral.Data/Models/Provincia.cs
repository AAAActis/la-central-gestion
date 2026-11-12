using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Provincias argentinas en uso. Elimina la dependencia transitiva de las dos tablas de dirección, donde el nombre viajaba como texto libre por fila. Hallazgo N-5. El centinela «&lt; Prov.NO Registrada» del sistema anterior no se carga: su equivalente acá es la clave ajena en NULL.
/// </summary>
public partial class Provincia
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<ClienteDireccion> ClienteDireccion { get; set; } = new List<ClienteDireccion>();

    public virtual ICollection<ProveedorDireccion> ProveedorDireccion { get; set; } = new List<ProveedorDireccion>();
}

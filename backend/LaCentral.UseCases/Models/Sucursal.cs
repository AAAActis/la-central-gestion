using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

public partial class Sucursal
{
    public short Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public virtual ICollection<FacturaCompra> FacturaCompras { get; set; } = new List<FacturaCompra>();

    public virtual ICollection<FacturaVentum> FacturaVenta { get; set; } = new List<FacturaVentum>();

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<Transferencium> TransferenciumSucursalDestinos { get; set; } = new List<Transferencium>();

    public virtual ICollection<Transferencium> TransferenciumSucursalOrigens { get; set; } = new List<Transferencium>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}

using System;
using System.Collections.Generic;

namespace LaCentral.UseCases.Models;

/// <summary>
/// En La Central se usan tres márgenes: 50 %, 40 % y 0,01 %. El último representa precio de venta igual al costo, porque el sistema anterior no admite cargar cero.
/// </summary>
public partial class ArticuloMargen
{
    public int ArticuloId { get; set; }

    public short Numero { get; set; }

    public decimal Porcentaje { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;
}

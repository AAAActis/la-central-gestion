using System;
using System.Collections.Generic;

namespace LaCentral.Data.Models;

/// <summary>
/// Existencia por artículo y ubicación. Admite valores negativos: la venta sin stock no se bloquea (HU-STK-03).
/// </summary>
public partial class Stock
{
    public int ArticuloId { get; set; }

    public short SucursalId { get; set; }

    public decimal Cantidad { get; set; }

    /// <summary>
    /// Posición física del artículo en el depósito de ESA sucursal. Estaba en `articulo`, donde un solo campo tenía que servir para todos los locales. Migró con el valor replicado desde articulo: en el origen el dato era único para todas las sucursales, así que la réplica conserva la información que había. Hallazgo N-9.
    /// </summary>
    public string? UbicacionDeposito { get; set; }

    /// <summary>
    /// Punto de reposición de ese artículo en ESA sucursal. 0 = sin piso definido, y por eso no genera alerta: un piso inventado produciría alertas falsas para todo el catálogo. Va en stock y no en articulo porque el mínimo es por sucursal. Pedido de la Dirección, 02/11/2026. Alimenta el reporte R-11.
    /// </summary>
    public decimal Minimo { get; set; }

    public virtual Articulo Articulo { get; set; } = null!;

    public virtual Sucursal Sucursal { get; set; } = null!;
}

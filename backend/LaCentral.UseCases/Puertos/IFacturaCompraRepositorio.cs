using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.UseCases.Puertos;

/// <summary>HU-FCO-01 · Registro de la factura de compra y de sus efectos.</summary>
public interface IFacturaCompraRepositorio
{
    /// <summary>
    /// La factura ACTIVA con ese número para ese proveedor, o null (CA2). Es la
    /// validación previa: ante concurrencia decide ux_factura_compra_numero_activa.
    /// </summary>
    Task<FacturaCompraExistente?> ObtenerNumeroActivoAsync(int proveedorId, string numero, CancellationToken ct = default);

    /// <summary>Id → activo de los artículos pedidos. Los que no existen no aparecen.</summary>
    Task<IReadOnlyDictionary<int, bool>> ObtenerEstadoArticulosAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    /// <summary>
    /// Escribe la cabecera y el detalle, incrementa el stock del destino de cada línea
    /// (D9), actualiza costo y último proveedor y agrega el historial de compra, SOLO
    /// de los artículos comprados. Se llama dentro de IUnidadDeTrabajo: si algo falla,
    /// no queda nada. Devuelve el Id que generó la base.
    /// </summary>
    Task<int> RegistrarConEfectosAsync(FacturaCompra factura, CancellationToken ct = default);
}

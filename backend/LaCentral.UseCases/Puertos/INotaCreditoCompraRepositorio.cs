using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;

namespace LaCentral.UseCases.Puertos;

/// <summary>HU-FCO-07 (057) · Registro de la nota de crédito de proveedor y de sus efectos.</summary>
public interface INotaCreditoCompraRepositorio
{
    /// <summary>
    /// Si el proveedor ya tiene una nota de crédito con ese número. Es la validación previa:
    /// ante concurrencia decide ux_nota_credito_compra_numero_activa (solo entre vigentes).
    /// </summary>
    Task<bool> ExisteNumeroAsync(int proveedorId, string numero, CancellationToken ct = default);

    /// <summary>Las facturas pedidas que existen, con su proveedor y si están activas.</summary>
    Task<IReadOnlyList<FacturaParaNotaCredito>> ObtenerFacturasAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default);

    /// <summary>
    /// Escribe la cabecera, su relación con las facturas y, en una devolución, sus líneas, y
    /// resta del stock del origen de cada línea. No toca costo, precios ni historial de compra.
    /// Se llama dentro de IUnidadDeTrabajo. Devuelve el Id que generó la base.
    /// </summary>
    Task<Result<int>> RegistrarConEfectosAsync(NotaCreditoCompra nota, CancellationToken ct = default);
}

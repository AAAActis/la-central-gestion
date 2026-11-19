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
    /// CA-014 (D-S6-25): bloquea las facturas (FOR UPDATE, para que dos devoluciones simultáneas
    /// no pasen juntas el límite) y devuelve, por cada artículo que figura en ellas, lo comprado en
    /// esas facturas y lo ya devuelto en notas de crédito vigentes que referencian alguna. Se llama
    /// dentro de IUnidadDeTrabajo.
    /// </summary>
    Task<IReadOnlyList<CompradoYDevuelto>> ObtenerCompradoYDevueltoAsync(IReadOnlyCollection<int> facturaIds, CancellationToken ct = default);

    /// <summary>
    /// Escribe la cabecera, su relación con las facturas y, en una devolución, sus líneas, y
    /// resta del stock del origen de cada línea. No toca costo, precios ni historial de compra.
    /// Se llama dentro de IUnidadDeTrabajo. Devuelve el Id que generó la base.
    /// </summary>
    Task<Result<int>> RegistrarConEfectosAsync(NotaCreditoCompra nota, CancellationToken ct = default);

    /// <summary>
    /// CA-011 (D-S6-22): bloquea la factura (FOR UPDATE, el mismo bloqueo que toma el registro de una
    /// nota) y devuelve los números de sus notas de crédito vigentes. Se llama dentro de IUnidadDeTrabajo.
    /// </summary>
    Task<IReadOnlyList<string>> ObtenerVigentesDeFacturaAsync(int facturaId, CancellationToken ct = default);

    /// <summary>CA-012: la nota con su número y si está vigente, o null si no existe.</summary>
    Task<NotaCreditoParaAnular?> ObtenerParaAnularAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// CA-012 (D-S6-23): marca la nota ANULADA con motivo, fecha, quién la pidió y quién la autorizó
    /// y, si era por devolución, suma al stock del origen de cada línea lo que había restado. Se llama
    /// dentro de IUnidadDeTrabajo.
    /// </summary>
    Task<Result> AnularConEfectosAsync(AnulacionNotaCreditoCompra anulacion, CancellationToken ct = default);

    /// <summary>CA-012: la página pedida del listado, de la más reciente a la más vieja, y el total.</summary>
    Task<(IReadOnlyList<NotaCreditoCompraResumen> Items, int Total)> BuscarAsync(
        FiltroNotasCreditoCompra filtro, int pagina, int tamanoPagina, CancellationToken ct = default);
}

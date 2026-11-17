using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>
/// HU-FCO-05 (029) · Interpreta el PDF del proveedor y propone la factura para revisar. Solo
/// lee: no guarda nada (CA-003) ni crea artículos (CA-004, RF-011).
/// </summary>
public class InterpretarFacturaPdfUseCase
{
    private const string CargaManual = "Cargá la factura por la vía manual.";

    private readonly ILectorTextoPdf _lector;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IFacturaCompraRepositorio _facturas;

    public InterpretarFacturaPdfUseCase(ILectorTextoPdf lector, IProveedorRepositorio proveedores, IFacturaCompraRepositorio facturas)
        => (_lector, _proveedores, _facturas) = (lector, proveedores, facturas);

    /// <param name="nombreArchivo">El nombre con el que se subió: hay layouts que no traen el número en el texto.</param>
    /// <param name="proveedorElegido">El proveedor que el operador eligió a mano, cuando el PDF no trae uno reconocible.</param>
    public async Task<Result<FacturaPdfInterpretada>> EjecutarAsync(
        byte[] archivo, string? nombreArchivo = null, int? proveedorElegido = null, CancellationToken ct = default)
    {
        // CA-002: ni un archivo que no es PDF ni un escaneado se interpretan.
        var texto = _lector.LeerRenglones(archivo);
        if (!texto.IsSuccess)
            return Fallo($"El archivo no es un PDF. {CargaManual}");
        if (texto.Value.Count == 0)
            return Fallo($"El PDF está escaneado, sin texto: el sistema no lo puede leer. {CargaManual}");
        if (LayoutsPdf.Leer(texto.Value, nombreArchivo) is not { } leido)
            return Fallo($"El sistema no reconoce el formato de este comprobante. {CargaManual}");

        // Un proveedor elegido a mano manda: el PDF no trae uno reconocible (Fazio no trae CUIT).
        int? proveedorId = null;
        if (proveedorElegido is { } elegido)
        {
            if (await _proveedores.ObtenerDetallePorIdAsync(elegido, ct) is not { Activo: true } proveedor)
                return Fallo($"El proveedor {elegido} no existe o está dado de baja.");
            proveedorId = proveedor.Id;
        }
        // Si no, es el dueño de uno de los CUIT del texto (el otro es el de la empresa).
        else foreach (var cuit in leido.Cuits)
            if (await _proveedores.ObtenerPorCuitAsync(cuit, ct) is { Activo: true } proveedor)
            {
                proveedorId = proveedor.Id;
                break;
            }

        // CA-004: sin proveedor no hay de quién buscar los códigos; ninguna línea se resuelve.
        var articulos = proveedorId is { } id
            ? await _facturas.ObtenerArticulosPorCodigoProveedorAsync(id, leido.Lineas.Select(l => l.CodigoProveedor).Distinct().ToArray(), ct)
            : new Dictionary<string, int>();

        var lineas = leido.Lineas.Select(l => new LineaPdfInterpretada(
            l.CodigoProveedor, l.Descripcion, articulos.TryGetValue(l.CodigoProveedor, out var articuloId) ? articuloId : null,
            l.Cantidad, l.PrecioUnitario, l.Descuento1, l.Descuento2, l.Descuento3)).ToList();
        IReadOnlyList<string> revisar = proveedorId is null ? [.. leido.ParaRevisar, "proveedorId"] : leido.ParaRevisar;

        return Result<FacturaPdfInterpretada>.Success(new FacturaPdfInterpretada(
            proveedorId, leido.TipoComprobante, leido.Numero, leido.FechaEmision, leido.FechaVencimiento,
            leido.PercepcionIva, leido.PercepcionIibb, leido.PercepcionMunicipal, leido.PercepcionGanancias, lineas, revisar));
    }

    private static Result<FacturaPdfInterpretada> Fallo(string error) => Result<FacturaPdfInterpretada>.Failure(TipoError.Invalido, error);
}

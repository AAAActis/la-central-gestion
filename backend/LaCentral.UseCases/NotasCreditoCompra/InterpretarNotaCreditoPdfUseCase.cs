using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>
/// HU-FCO-07 (057) CA-010 · Interpreta el PDF de una nota de crédito de proveedor con el mecanismo
/// de la 029. Solo lee: no guarda nada.
/// </summary>
public class InterpretarNotaCreditoPdfUseCase
{
    private readonly ILectorTextoPdf _lector;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IFacturaCompraRepositorio _facturas;

    public InterpretarNotaCreditoPdfUseCase(ILectorTextoPdf lector, IProveedorRepositorio proveedores, IFacturaCompraRepositorio facturas)
        => (_lector, _proveedores, _facturas) = (lector, proveedores, facturas);

    private const string CargaManual = "Cargá la nota de crédito por la vía manual.";

    public async Task<Result<NotaCreditoPdfInterpretada>> EjecutarAsync(byte[] archivo, CancellationToken ct = default)
    {
        // Como el CA-002 de la 029: ni un archivo que no es PDF ni un escaneado se interpretan.
        var texto = _lector.LeerRenglones(archivo);
        if (!texto.IsSuccess)
            return Fallo($"El archivo no es un PDF. {CargaManual}");
        if (texto.Value.Count == 0)
            return Fallo($"El PDF está escaneado, sin texto: el sistema no lo puede leer. {CargaManual}");
        if (LayoutsPdf.LeerNotaCredito(texto.Value) is not { } leida)
            return Fallo($"El sistema no reconoce el formato de esta nota de crédito. {CargaManual}");

        // El proveedor es el dueño activo de uno de los CUIT del texto (el otro es el de la empresa).
        int? proveedorId = null;
        foreach (var cuit in leida.Cuits)
            if (await _proveedores.ObtenerPorCuitAsync(cuit, ct) is { Activo: true } proveedor)
            {
                proveedorId = proveedor.Id;
                break;
            }

        // Regla c: las facturas, del mismo proveedor y activas. Sin proveedor no hay dónde buscarlas.
        var facturaIds = new List<int>();
        var sinEncontrar = new List<string>();
        foreach (var numero in leida.FacturasAsociadas)
            if (proveedorId is { } id && await _facturas.ObtenerNumeroActivoAsync(id, numero, ct) is { } factura)
                facturaIds.Add(factura.Id);
            else
                sinEncontrar.Add(numero);

        var revisar = leida.ParaRevisar.ToList();
        if (proveedorId is null)
            revisar.Add("proveedorId");
        if (sinEncontrar.Count > 0 && !revisar.Contains("facturaIds"))
            revisar.Add("facturaIds");

        return Result<NotaCreditoPdfInterpretada>.Success(new(proveedorId, leida.Numero, leida.FechaEmision, leida.Clase,
            leida.ImporteNeto, facturaIds, sinEncontrar, revisar));
    }

    private static Result<NotaCreditoPdfInterpretada> Fallo(string error) =>
        Result<NotaCreditoPdfInterpretada>.Failure(TipoError.Invalido, error);
}

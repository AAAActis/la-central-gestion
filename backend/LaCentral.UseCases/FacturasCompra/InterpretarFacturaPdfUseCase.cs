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
    private readonly ILectorTextoPdf _lector;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IFacturaCompraRepositorio _facturas;

    public InterpretarFacturaPdfUseCase(ILectorTextoPdf lector, IProveedorRepositorio proveedores, IFacturaCompraRepositorio facturas)
        => (_lector, _proveedores, _facturas) = (lector, proveedores, facturas);

    public Task<Result<FacturaPdfInterpretada>> EjecutarAsync(byte[] archivo, CancellationToken ct = default)
        => Task.FromResult(Result<FacturaPdfInterpretada>.Failure(TipoError.Invalido, "Sin implementar."));
}

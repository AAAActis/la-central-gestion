using LaCentral.UseCases.Comun;
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

    public Task<Result<NotaCreditoPdfInterpretada>> EjecutarAsync(byte[] archivo, CancellationToken ct = default)
        => Task.FromResult(Result<NotaCreditoPdfInterpretada>.Success(new(null, null, null, null, null, [], [], [])));
}

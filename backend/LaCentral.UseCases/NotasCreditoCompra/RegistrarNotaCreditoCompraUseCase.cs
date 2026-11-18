using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) · Registro de nota de crédito de proveedor.</summary>
public class RegistrarNotaCreditoCompraUseCase
{
    private readonly INotaCreditoCompraRepositorio _notas;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IArticuloRepositorio _articulos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public RegistrarNotaCreditoCompraUseCase(INotaCreditoCompraRepositorio notas, IProveedorRepositorio proveedores,
        IFacturaCompraRepositorio facturas, IArticuloRepositorio articulos, IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
        => (_notas, _proveedores, _facturas, _articulos, _unidadDeTrabajo, _contexto) =
            (notas, proveedores, facturas, articulos, unidadDeTrabajo, contexto);

    public Task<Result<RegistrarNotaCreditoCompraResponse>> EjecutarAsync(
        RegistrarNotaCreditoCompraRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<RegistrarNotaCreditoCompraResponse>.Success(new(0)));

    public Result<TotalesNotaCreditoCompra> CalcularTotales(RegistrarNotaCreditoCompraRequest request)
        => Result<TotalesNotaCreditoCompra>.Success(new(0m, 0m, 0m, 0m));
}

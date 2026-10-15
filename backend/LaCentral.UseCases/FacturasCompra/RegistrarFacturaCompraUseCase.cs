using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-01 · Registro manual de factura de compra.</summary>
public class RegistrarFacturaCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IArticuloRepositorio _articulos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public RegistrarFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IProveedorRepositorio proveedores,
        IArticuloRepositorio articulos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
    {
        _facturas = facturas;
        _proveedores = proveedores;
        _articulos = articulos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public Task<Result<RegistrarFacturaCompraResponse>> EjecutarAsync(
        RegistrarFacturaCompraRequest request, CancellationToken ct = default)
        => Task.FromResult(Result<RegistrarFacturaCompraResponse>.Success(new RegistrarFacturaCompraResponse(0)));
}

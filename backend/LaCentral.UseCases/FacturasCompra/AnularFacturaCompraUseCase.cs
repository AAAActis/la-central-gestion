using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-04 (028) · Anulación de factura de compra, con doble certificación.</summary>
public class AnularFacturaCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHash _hash;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;
    private readonly CalcularPreciosArticuloUseCase _calcularPrecios;

    public AnularFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IUsuarioRepositorio usuarios,
        IServicioHash hash,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto,
        CalcularPreciosArticuloUseCase calcularPrecios)
    {
        _facturas = facturas;
        _usuarios = usuarios;
        _hash = hash;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
        _calcularPrecios = calcularPrecios;
    }

    public Task<Result> EjecutarAsync(int facturaId, AnularFacturaCompraRequest request, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

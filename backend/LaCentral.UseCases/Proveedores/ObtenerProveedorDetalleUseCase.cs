using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Proveedores;

/// <summary>045 · la ficha de un proveedor. Mismo criterio que ObtenerClienteDetalleUseCase.</summary>
public class ObtenerProveedorDetalleUseCase
{
    private readonly IProveedorRepositorio _proveedorRepositorio;

    public ObtenerProveedorDetalleUseCase(IProveedorRepositorio proveedorRepositorio)
        => _proveedorRepositorio = proveedorRepositorio;

    public async Task<Result<ProveedorDetalleDto>> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var p = await _proveedorRepositorio.ObtenerDetallePorIdAsync(id, ct);
        if (p is null)
            return Result<ProveedorDetalleDto>.Failure(TipoError.NoEncontrado, "El proveedor indicado no existe.");

        return Result<ProveedorDetalleDto>.Success(new ProveedorDetalleDto(
            p.Id, p.Codigo, p.RazonSocial, p.Cuit, p.UrlReferencia, p.Activo, p.MotivoBaja, p.Telefonos, p.Direcciones,
            p.TipoComprobanteHabitual));
    }
}

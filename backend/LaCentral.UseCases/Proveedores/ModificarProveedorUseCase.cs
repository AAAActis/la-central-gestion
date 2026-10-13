using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Proveedores;

public class ModificarProveedorUseCase
{
    private readonly IProveedorRepositorio _proveedorRepositorio;

    public ModificarProveedorUseCase(IProveedorRepositorio proveedorRepositorio)
    {
        _proveedorRepositorio = proveedorRepositorio;
    }

    public async Task<Result> EjecutarAsync(int id, ModificarProveedorRequest request, CancellationToken ct = default)
    {
        var proveedor = await _proveedorRepositorio.ObtenerDetallePorIdAsync(id, ct);
        if (proveedor == null)
        {
            return Result.Failure(TipoError.NoEncontrado, "El proveedor indicado no existe.");
        }

        // CA-004: Bloquear si está dado de baja
        if (!proveedor.Activo)
        {
            return Result.Failure(TipoError.Invalido, "El proveedor está dado de baja. Reactívelo antes de modificarlo.");
        }

        if (string.IsNullOrWhiteSpace(request.RazonSocial) || string.IsNullOrWhiteSpace(request.Cuit))
        {
            return Result.Failure(TipoError.Invalido, "Razón Social y CUIT son obligatorios.");
        }

        if (!string.IsNullOrWhiteSpace(request.UrlReferencia) && !Uri.TryCreate(request.UrlReferencia, UriKind.Absolute, out _))
        {
            return Result.Failure(TipoError.Invalido, "La URL de referencia no tiene un formato válido. Debe incluir http:// o https://");
        }

        // CA-002: CUIT duplicado de otro proveedor
        if (!string.IsNullOrWhiteSpace(request.Cuit) && request.Cuit != proveedor.Cuit)
        {
            var proveedorExistente = await _proveedorRepositorio.ObtenerPorCuitAsync(request.Cuit, ct);
            if (proveedorExistente != null && proveedorExistente.Id != proveedor.Id)
            {
                return Result.Failure(TipoError.Conflicto, $"El CUIT ingresado ya pertenece al proveedor: {proveedorExistente.RazonSocial}.");
            }
        }

        proveedor.RazonSocial = request.RazonSocial.Trim();
        proveedor.Cuit = request.Cuit.Trim();
        proveedor.UrlReferencia = request.UrlReferencia?.Trim();
        proveedor.Telefonos = (request.Telefonos ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        proveedor.Direcciones = (request.Direcciones ?? new List<string>()).Where(d => !string.IsNullOrWhiteSpace(d)).ToList();

        await _proveedorRepositorio.ActualizarAsync(proveedor, ct);

        return Result.Success();
    }
}
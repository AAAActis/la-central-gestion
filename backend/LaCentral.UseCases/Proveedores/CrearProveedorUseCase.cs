using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Proveedores;

public class CrearProveedorUseCase
{
    private readonly IProveedorRepositorio _proveedorRepositorio;

    public CrearProveedorUseCase(IProveedorRepositorio proveedorRepositorio)
    {
        _proveedorRepositorio = proveedorRepositorio;
    }

    public async Task<Result<CrearProveedorResponse>> EjecutarAsync(CrearProveedorRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RazonSocial) || string.IsNullOrWhiteSpace(request.Cuit))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Invalido, "Razón Social y CUIT son obligatorios.");
        }

        // CA-004 y CA-005: Validar formato de URL absoluta solo si se envió un valor
        if (!string.IsNullOrWhiteSpace(request.UrlReferencia) && !Uri.TryCreate(request.UrlReferencia, UriKind.Absolute, out _))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Invalido, "La URL de referencia no tiene un formato válido. Debe incluir http:// o https://");
        }

        // CA-002: CUIT duplicado rechaza la operación
        if (await _proveedorRepositorio.ExisteCuitAsync(request.Cuit, ct))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Conflicto, "Ya existe un proveedor registrado con este CUIT.");
        }

        // CA-003: Razón social repetida no frena el alta, pero genera advertencia
        string? advertencia = null;
        if (await _proveedorRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro proveedor con la misma Razón Social.";
        }

        // CA-001: Se registra activo. Evitamos colecciones nulas (Hallazgo 4)
        var nuevoProveedor = new Proveedor
        {
            RazonSocial = request.RazonSocial.Trim(),
            Cuit = request.Cuit.Trim(),
            UrlReferencia = request.UrlReferencia?.Trim(),
            Activo = true,
            Telefonos = (request.Telefonos ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
            Direcciones = (request.Direcciones ?? new List<string>()).Where(d => !string.IsNullOrWhiteSpace(d)).ToList()
        };

        await _proveedorRepositorio.AgregarAsync(nuevoProveedor, ct);

        var response = new CrearProveedorResponse(nuevoProveedor.Cuit, advertencia);
        return Result<CrearProveedorResponse>.Success(response);
    }
}
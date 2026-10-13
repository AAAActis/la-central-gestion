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
        // Validación obligatoria: Código y Razón Social (CUIT es opcional)
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Invalido, "Código y Razón Social son obligatorios.");
        }

        if (!string.IsNullOrWhiteSpace(request.UrlReferencia) && !Uri.TryCreate(request.UrlReferencia, UriKind.Absolute, out _))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Invalido, "La URL de referencia no tiene un formato válido. Debe incluir http:// o https://");
        }

        // Solo validamos duplicado si enviaron un CUIT
        if (!string.IsNullOrWhiteSpace(request.Cuit) && await _proveedorRepositorio.ExisteCuitAsync(request.Cuit, ct))
        {
            return Result<CrearProveedorResponse>.Failure(TipoError.Conflicto, "Ya existe un proveedor registrado con este CUIT.");
        }

        string? advertencia = null;
        if (await _proveedorRepositorio.ExisteRazonSocialAsync(request.RazonSocial, ct))
        {
            advertencia = "Advertencia: Ya existe otro proveedor con la misma Razón Social.";
        }

        var nuevoProveedor = new Proveedor
        {
            Codigo = request.Codigo.Trim(),
            RazonSocial = request.RazonSocial.Trim(),
            Cuit = string.IsNullOrWhiteSpace(request.Cuit) ? null : request.Cuit.Trim(),
            UrlReferencia = request.UrlReferencia?.Trim(),
            Activo = true,
            Telefonos = (request.Telefonos ?? new List<string>()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList(),
            Direcciones = (request.Direcciones ?? new List<string>()).Where(d => !string.IsNullOrWhiteSpace(d)).ToList()
        };

        await _proveedorRepositorio.AgregarAsync(nuevoProveedor, ct);

        var response = new CrearProveedorResponse(nuevoProveedor.Codigo, advertencia);
        return Result<CrearProveedorResponse>.Success(response);
    }
}
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Models;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases;

public class CrearUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IServicioHash _servicioHash;

    public CrearUsuarioUseCase(IUsuarioRepositorio usuarioRepositorio, IServicioHash servicioHash)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _servicioHash = servicioHash;
    }

    public async Task<Result> EjecutarAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validar Sucursal (Regla de negocio estricta del contexto)
        if (request.Sucursal != "FR" && request.Sucursal != "SV")
        {
            return Result.Failure(TipoError.Invalido, "La sucursal debe ser FR (Fragueiro) o SV (San Vicente).");
        }

        // 2. Validar Rol
        if (string.IsNullOrWhiteSpace(request.Rol))
        {
            return Result.Failure(TipoError.Invalido, "El rol es obligatorio.");
        }

        // 3. Validar Nombre Único
        var existe = await _usuarioRepositorio.ExisteNombreUsuarioAsync(request.NombreUsuario, cancellationToken);
        if (existe)
        {
            return Result.Failure(TipoError.Conflicto, "El nombre de usuario ya está registrado.");
        }

        // 4. Hashear la contraseña
        var passwordHash = _servicioHash.HashearClave(request.Password);

        // 5. Armar el modelo de dominio/DTO para el repositorio
        int idSucursal = request.Sucursal == "FR" ? 1 : 2; 

        // Mapeo de rol contra los valores reales de la tabla `rol`:
        //   1 = OPERADOR   2 = ADMINISTRADOR
        // Estaba invertido, y el valor por defecto creaba un ADMINISTRADOR
        // ante cualquier string no reconocido. Ahora un rol desconocido se
        // rechaza: ante la duda, no se otorga privilegio.
        int idRol = request.Rol.Trim().ToUpperInvariant() switch
        {
            "OPERADOR" => 1,
            "ADMINISTRADOR" => 2,
            _ => 0
        };

        if (idRol == 0)
        {
            return Result.Failure(TipoError.Invalido, "El rol debe ser OPERADOR o ADMINISTRADOR.");
        }

        var nuevoUsuario = new Usuario
        {
            NombreUsuario = request.NombreUsuario,
            HashContrasena = passwordHash,
            RolId = idRol,
            SucursalId = idSucursal,
            Activo = true // Por defecto arranca activo
        };

        // 6. Persistir
        await _usuarioRepositorio.CrearAsync(nuevoUsuario, cancellationToken);

        return Result.Success();
    }
}
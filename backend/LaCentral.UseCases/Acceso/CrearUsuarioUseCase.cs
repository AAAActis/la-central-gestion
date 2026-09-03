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
            return Result.Failure("La sucursal debe ser FR (Fragueiro) o SV (San Vicente).");
        }

        // 2. Validar Rol (acá poné los roles exactos que hayan definido, ej: Admin, Vendedor)
        if (string.IsNullOrWhiteSpace(request.Rol))
        {
            return Result.Failure("El rol es obligatorio.");
        }

        // 3. Validar Nombre Único
        var existe = await _usuarioRepositorio.ExisteNombreUsuarioAsync(request.NombreUsuario, cancellationToken);
        if (existe)
        {
            return Result.Failure("El nombre de usuario ya está registrado.");
        }

        // 4. Hashear la contraseña (delegado al puerto que implementa Santi)
        var passwordHash = _servicioHash.HashearClave(request.Password);

        // 5. Armar el modelo de dominio/DTO para el repositorio
        int idSucursal = request.Sucursal == "FR" ? 1 : 2; 

        // Mapeo de Rol
        int idRol = request.Rol.ToUpper() switch
        {
            "ADMINISTRADOR" => 2,
            "OPERADOR" => 1,
            _ => 0 // 0 indica inválido
        };
        if (idRol == 0)
        {
            return Result.Failure("Rol inválido. Debe ser ADMINISTRADOR u OPERADOR.");
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
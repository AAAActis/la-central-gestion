using LaCentral.UseCases.Models;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases;

public class AutenticarUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IServicioHash _servicioHash;
    private readonly IGeneradorToken _generadorToken;

    public AutenticarUsuarioUseCase(
        IUsuarioRepositorio usuarioRepositorio, 
        IServicioHash servicioHash, 
        IGeneradorToken generadorToken)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _servicioHash = servicioHash;
        _generadorToken = generadorToken;
    }

    public async Task<Result<AuthenticarUsuarioResponse>> EjecutarAsync(AuthenticarUsuarioRequest request, CancellationToken ct = default)
    {
        // Buscamos al usuario por el puerto
        var usuario = await _usuarioRepositorio.ObtenerPorNombreAsync(request.NombreUsuario, ct);

        // CA2: Mensaje genérico si no existe
        if (usuario == null)
        {
            return Result<AuthenticarUsuarioResponse>.Failure("Usuario o contraseña incorrectos.");
        }

        // CA2: Mensaje genérico si la contraseña no coincide
        var claveValida = _servicioHash.VerificarClave(request.Contrasena, usuario.HashContrasena);
        if (!claveValida)
        {
            // CA4: No llevamos contador de intentos, solo rechazamos
            return Result<AuthenticarUsuarioResponse>.Failure("Usuario o contraseña incorrectos.");
        }

        // CA3: Denegar si está dado de baja (validando el campo Activo)
        if (!usuario.Activo)
        {
            return Result<AuthenticarUsuarioResponse>.Failure("El usuario no tiene acceso al sistema.");
        }

        // Mapeo inverso de Rol (ajustá los números según los IDs reales de tu base)
        string nombreRol = usuario.RolId switch
        {
            1 => "Admin",
            2 => "Operador",
            _ => "Desconocido"
        };

        // Mapeo inverso de Sucursal (asumiendo 1 = FR, 2 = SV)
        string codigoSucursal = usuario.SucursalId == 1 ? "FR" : "SV";

        // CA1: Todo correcto. Generamos el token delegando al puerto y pasándole los 2 parámetros que pide Santi.
        var token = _generadorToken.GenerarToken(
            usuario.Id, 
            usuario.NombreUsuario, 
            "Admin", // O la variable que contenga el nombre del rol (ej: usuario.Rol.Nombre)
            (short)usuario.SucursalId
        );

        // CA5: Retornamos la respuesta usando los strings que acabamos de mapear
        var response = new AuthenticarUsuarioResponse
        {
            Token = token,
            Rol = nombreRol,                 
            NombreUsuario = usuario.NombreUsuario,
            Sucursal = codigoSucursal        
        };

        return Result<AuthenticarUsuarioResponse>.Success(response);
    }
}
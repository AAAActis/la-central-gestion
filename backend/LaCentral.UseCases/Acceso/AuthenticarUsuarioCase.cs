using LaCentral.UseCases.Acceso.Dtos;
using LaCentral.UseCases.Models;
using LaCentral.UseCases.Puertos;

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

    public async Task<Result<SesionDto>> EjecutarAsync(AuthenticarUsuarioRequest request, CancellationToken ct = default)
    {
        // Buscamos al usuario por el puerto
        var usuario = await _usuarioRepositorio.ObtenerPorNombreAsync(request.NombreUsuario, ct);

        // CA2: Mensaje generico si no existe
        if (usuario == null)
        {
            return Result<SesionDto>.Failure("Usuario o contraseña incorrectos.");
        }

        // CA2: Mensaje generico si la contraseña no coincide
        var claveValida = _servicioHash.VerificarClave(request.Contrasena, usuario.HashContrasena);
        if (!claveValida)
        {
            // CA4: No llevamos contador de intentos, solo rechazamos
            return Result<SesionDto>.Failure("Usuario o contraseña incorrectos.");
        }

        // CA3: Denegar si está dado de baja (validando el campo Activo)
        if (!usuario.Activo)
        {
            return Result<SesionDto>.Failure("El usuario no tiene acceso al sistema.");
        }

        // Mapeo inverso de Rol (ajustá los números según los IDs reales de tu base)
        string nombreRol = usuario.RolId switch
        {
            1 => "Admin",
            2 => "Operador",
            _ => "Desconocido"
        };

        // CA1: Todo correcto. Generamos el token delegando al puerto y pasándole los 2 parámetros que pide Santi.
        var token = _generadorToken.GenerarToken(usuario, nombreRol);

        // CA5: Retornamos el record SesionDto (sin mapear la sucursal a string porque pide el short directo)
        var response = new SesionDto(
            token,
            usuario.NombreUsuario,
            nombreRol,
            (short)usuario.SucursalId
        );

        return Result<SesionDto>.Success(response);
    }
}
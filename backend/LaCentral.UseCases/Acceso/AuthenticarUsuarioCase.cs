using LaCentral.UseCases.Acceso.Dtos;
using LaCentral.UseCases.Comun;
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

        // Mapeo inverso de Rol con los strings exactos de la base
        string nombreRol = usuario.RolId switch
        {
            2 => "ADMINISTRADOR",
            1 => "OPERADOR",
            _ => "OPERADOR" // Ante la duda, siempre otorgar el menor privilegio posible
        };

        // CA1: Generamos el token pasándole los 4 parámetros exactos que pide la nueva interfaz
        var token = _generadorToken.GenerarToken(
            usuario.Id, 
            usuario.NombreUsuario, 
            nombreRol, 
            (short)usuario.SucursalId
        );

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
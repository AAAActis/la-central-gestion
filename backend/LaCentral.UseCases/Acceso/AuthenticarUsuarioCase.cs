using LaCentral.UseCases.Acceso.Dtos;
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

    public async Task<Result<SesionDto>> EjecutarAsync(AuthenticarUsuarioRequest request, CancellationToken ct = default)
    {
        // Buscamos al usuario por el puerto
        var usuario = await _usuarioRepositorio.ObtenerPorNombreAsync(request.NombreUsuario, ct);

        // CA2: Mensaje generico si no existe
        if (usuario == null)
        {
            return Result<SesionDto>.Failure(TipoError.NoAutorizado, "Usuario o contraseña incorrectos.");
        }

        // CA2: Mensaje generico si la contraseña no coincide
        var claveValida = _servicioHash.VerificarClave(request.Contrasena, usuario.HashContrasena);
        if (!claveValida)
        {
            // CA4: No llevamos contador de intentos, solo rechazamos
            return Result<SesionDto>.Failure(TipoError.NoAutorizado, "Usuario o contraseña incorrectos.");
        }

        // CA3: Denegar si está dado de baja (validando el campo Activo)
        if (!usuario.Activo)
        {
            return Result<SesionDto>.Failure(TipoError.NoAutorizado, "El usuario no tiene acceso al sistema.");
        }

        // Mapeo de rol contra los valores reales de la tabla `rol`:
        //   1 = OPERADOR   2 = ADMINISTRADOR
        // Estaba invertido: un operador recibía un token que lo declaraba
        // administrador. Los nombres son los de la base, tal cual, porque
        // [Authorize(Roles = "...")] compara el string exacto del claim.
        string nombreRol = usuario.RolId switch
        {
            1 => "OPERADOR",
            2 => "ADMINISTRADOR",
            _ => "DESCONOCIDO"   // no coincide con ningún [Authorize]: no habilita nada
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
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Models;
using LaCentral.UseCases.Puertos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases;

public class CrearUsuarioUseCase
{
    private readonly IUsuarioRepositorio _usuarioRepositorio;
    private readonly IServicioHash _servicioHash;

    private const int LargoMinimoClave = 6;
    private const int LargoMaximoNombreUsuario = 30;

    public CrearUsuarioUseCase(IUsuarioRepositorio usuarioRepositorio, IServicioHash servicioHash)
    {
        _usuarioRepositorio = usuarioRepositorio;
        _servicioHash = servicioHash;
    }

    public async Task<Result> EjecutarAsync(CrearUsuarioRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validar Nombre de Usuario (Hallazgo 2)
        // La columna es varchar(30) NOT NULL UNIQUE. Sin este chequeo, un
        // nombre de 31 caracteres llega hasta SaveChangesAsync y PostgreSQL
        // lo rechaza con un 500 en vez de un 400.
        if (string.IsNullOrWhiteSpace(request.NombreUsuario) || request.NombreUsuario.Length > LargoMaximoNombreUsuario)
        {
            return Result.Failure(TipoError.Invalido,
                $"El nombre de usuario es obligatorio y no puede superar los {LargoMaximoNombreUsuario} caracteres.");
        }

        // 2. Validar Contraseña (Hallazgo 1)
        // Mismo criterio y mismo mínimo que RestablecerContrasenaUseCase,
        // para no tener dos reglas distintas de "contraseña válida".
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < LargoMinimoClave)
        {
            return Result.Failure(TipoError.Invalido,
                $"La contraseña debe tener al menos {LargoMinimoClave} caracteres.");
        }

        // 3. Validar Sucursal (Hallazgo 6: normalización unificada)
        // Antes se comparaba exacto ("FR"/"SV") mientras que el Rol se
        // normalizaba con Trim().ToUpperInvariant(): "fr" en minúscula se
        // rechazaba para Sucursal pero "operador" en minúscula se aceptaba
        // para Rol. Ahora los dos campos usan el mismo criterio.
        var sucursalNormalizada = request.Sucursal.Trim().ToUpperInvariant();
        if (sucursalNormalizada != "FR" && sucursalNormalizada != "SV")
        {
            return Result.Failure(TipoError.Invalido, "La sucursal debe ser FR (Fragueiro) o SV (San Vicente).");
        }

        // 4. Validar Nombre Único
        var existe = await _usuarioRepositorio.ExisteNombreUsuarioAsync(request.NombreUsuario, cancellationToken);
        if (existe)
        {
            return Result.Failure(TipoError.Conflicto, "El nombre de usuario ya está registrado.");
        }

        // 5. Hashear la contraseña
        var passwordHash = _servicioHash.HashearClave(request.Password);

        // 6. Armar el modelo de dominio/DTO para el repositorio
        int idSucursal = sucursalNormalizada == "FR" ? 1 : 2;

        // Mapeo de rol contra los valores reales de la tabla `rol`:
        //   1 = OPERADOR   2 = ADMINISTRADOR
        // Un rol desconocido se rechaza acá mismo (Hallazgo 7): antes había
        // además un chequeo separado de "rol vacío" más arriba que decía
        // lo mismo con otro mensaje — se saca, queda un solo lugar que
        // rechaza un rol inválido, con un solo mensaje.
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

        // 7. Persistir
        await _usuarioRepositorio.CrearAsync(nuevoUsuario, cancellationToken);

        return Result.Success();
    }
}

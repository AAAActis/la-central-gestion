using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases;

/// <summary>
/// El Administrador asigna una contraseña nueva sin necesitar la anterior.
/// Cubre CA5 de HU-ACC-02.
/// </summary>
public class RestablecerContrasenaUseCase
{
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHash _hash;

    private const int LargoMinimoClave = 6;

    public RestablecerContrasenaUseCase(IUsuarioRepositorio usuarios, IServicioHash hash)
    {
        _usuarios = usuarios;
        _hash = hash;
    }

    public async Task<Result> EjecutarAsync(
        int idUsuario, string claveNueva, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(claveNueva) || claveNueva.Length < LargoMinimoClave)
        {
            return Result.Failure(TipoError.Invalido,
                $"La contraseña debe tener al menos {LargoMinimoClave} caracteres.");
        }

        var usuario = await _usuarios.ObtenerPorIdAsync(idUsuario, cancellationToken);
        if (usuario is null)
        {
            return Result.Failure(TipoError.NoEncontrado,
                "El usuario indicado no existe.");
        }

        // Se guarda el hash, nunca la contraseña. El sistema no necesita
        // conocerla: solo verificar después que coincida.
        usuario.HashContrasena = _hash.HashearClave(claveNueva);

        await _usuarios.ActualizarAsync(usuario, cancellationToken);

        return Result.Success();
    }
}

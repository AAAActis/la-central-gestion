using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Models;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
// La gestión de usuarios queda reservada al Administrador: CA2 y CA3 de
// HU-ACC-03. Va sobre la clase y no sobre cada método, así cualquier
// endpoint que se agregue después queda protegido por omisión — la
// alternativa, anotar método por método, se olvida.
//
// El string es el valor exacto de la tabla `rol`. [Authorize] compara el
// claim tal cual: ni normaliza mayúsculas ni ignora acentos.
[Authorize(Roles = "ADMINISTRADOR")]
public class UsuariosController : ControllerBase
{
    private readonly CrearUsuarioUseCase _crearUsuario;
    private readonly DarDeBajaUsuarioUseCase _darDeBaja;
    private readonly ReactivarUsuarioUseCase _reactivar;
    private readonly RestablecerContrasenaUseCase _restablecerContrasena;

    // Se inyecta la CLASE CONCRETA del caso de uso, sin interfaz de por medio.
    // Es una decisión de Core-Driven: las interfaces se reservan para los
    // puertos de infraestructura. Para la lógica de negocio no aportan nada
    // y solo agregan un salto más al leer el código.
    public UsuariosController(
        CrearUsuarioUseCase crearUsuario,
        DarDeBajaUsuarioUseCase darDeBaja,
        ReactivarUsuarioUseCase reactivar,
        RestablecerContrasenaUseCase restablecerContrasena)
    {
        _crearUsuario = crearUsuario;
        _darDeBaja = darDeBaja;
        _reactivar = reactivar;
        _restablecerContrasena = restablecerContrasena;
    }

    /// <summary>Alta de usuario. Cubre CA1 y CA2 de HU-ACC-02.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        CrearUsuarioRequest request, CancellationToken cancellationToken)
    {
        // Traducción del contrato público al contrato del núcleo. Es el
        // trabajo del controlador: la entidad y el DTO interno nunca se
        // exponen, y el cliente nunca depende de cómo se llama algo adentro.
        var entrada = new DtosNucleo.CrearUsuarioRequest
        {
            NombreUsuario = request.NombreUsuario,
            Password      = request.Clave,
            Rol           = request.Rol,
            Sucursal      = request.Sucursal
        };

        var resultado = await _crearUsuario.EjecutarAsync(entrada, cancellationToken);

        return this.AResultadoHttp(resultado);
    }

    /// <summary>Baja lógica. Cubre CA3 y CA4 de HU-ACC-02.</summary>
    // Se usa POST sobre un sub-recurso de acción y no DELETE porque acá no se
    // borra nada: se cambia un estado y hace falta mandar un cuerpo con el
    // motivo, cosa que DELETE no contempla bien.
    [HttpPost("{id:int}/baja")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DarDeBaja(
        int id, DarDeBajaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _darDeBaja.EjecutarAsync(id, request.Motivo, cancellationToken);

        return this.AResultadoHttp(resultado);
    }

    /// <summary>Reactivación de un usuario dado de baja.</summary>
    [HttpPost("{id:int}/reactivacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reactivar(
        int id, CancellationToken cancellationToken)
    {
        var resultado = await _reactivar.EjecutarAsync(id, cancellationToken);

        return this.AResultadoHttp(resultado);
    }

    /// <summary>Restablecimiento de contraseña por el Administrador. CA5 de HU-ACC-02.</summary>
    [HttpPost("{id:int}/contrasena")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestablecerContrasena(
        int id, RestablecerContrasenaRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _restablecerContrasena
            .EjecutarAsync(id, request.ClaveNueva, cancellationToken);

        return this.AResultadoHttp(resultado);
    }
}

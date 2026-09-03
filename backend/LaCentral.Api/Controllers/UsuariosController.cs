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

    // Se inyecta la CLASE CONCRETA del caso de uso, sin interfaz de por medio.
    // Es una decisión de Core-Driven: las interfaces se reservan para los
    // puertos de infraestructura. Para la lógica de negocio no aportan nada
    // y solo agregan un salto más al leer el código.
    public UsuariosController(CrearUsuarioUseCase crearUsuario)
    {
        _crearUsuario = crearUsuario;
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
}

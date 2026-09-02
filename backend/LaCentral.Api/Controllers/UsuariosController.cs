using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Models;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
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

    /// <summary>Alta de usuario. Cubre CA-001 y CA-002 de HU-ACC-02.</summary>
    // TODO (#38, martes): sumar [Authorize(Roles = "...")] cuando esté
    // acordado el vocabulario de roles contra la tabla `rol` de la base.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

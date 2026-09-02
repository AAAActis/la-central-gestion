using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Models;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/acceso")]
public class AccesoController : ControllerBase
{
    private readonly AutenticarUsuarioUseCase _autenticar;

    public AccesoController(AutenticarUsuarioUseCase autenticar)
    {
        _autenticar = autenticar;
    }

    /// <summary>Inicio de sesión. Cierra CA-001, CA-002 y CA-003 de HU-ACC-01.</summary>
    [HttpPost("login")]
    // Hoy es redundante porque todavía no hay política global de
    // autenticación, pero deja escrita la intención: el login es el único
    // endpoint que no puede exigir estar autenticado. Cuando el martes se
    // protejan los controladores, este atributo es el que evita el
    // círculo de pedir token para poder pedir token.
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> IniciarSesion(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var entrada = new DtosNucleo.AuthenticarUsuarioRequest
        {
            NombreUsuario = request.NombreUsuario,
            Contrasena    = request.Clave
        };

        var resultado = await _autenticar.EjecutarAsync(entrada, cancellationToken);

        return this.AResultadoHttp(resultado);
    }
}

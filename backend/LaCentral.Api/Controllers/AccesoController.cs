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
    public async Task<IActionResult> IniciarSesion(LoginRequest req, CancellationToken ct)
    {
        // Empaquetamos los datos de la API en el request que espera tu caso de uso
        var requestNucleo = new DtosNucleo.AuthenticarUsuarioRequest
        { 
            NombreUsuario = req.NombreUsuario, 
            Contrasena = req.Contrasena 
        };

        var resultado = await _autenticar.EjecutarAsync(requestNucleo, ct);
        return this.AResultadoHttp(resultado);
    }
}

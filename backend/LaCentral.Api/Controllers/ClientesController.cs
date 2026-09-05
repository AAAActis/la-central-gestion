using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.Clientes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Clientes.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/clientes")]
// [Authorize] sin roles: cualquier puesto autenticado opera con clientes.
// Es el CA1 de HU-ACC-03 — el Operador accede a todo lo operativo, y solo
// la gestión de usuarios queda reservada al Administrador.
[Authorize]
public class ClientesController : ControllerBase
{
    private readonly CrearClienteUseCase _crearCliente;

    public ClientesController(CrearClienteUseCase crearCliente)
    {
        _crearCliente = crearCliente;
    }

    /// <summary>Alta de cliente con sus teléfonos y direcciones. HU-CLI-01.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        CrearClienteRequest request, CancellationToken cancellationToken)
    {
        var entrada = new DtosNucleo.CrearClienteRequest(
            request.Codigo,
            request.RazonSocial,
            request.Cuit,
            // El núcleo espera listas, no null. La ausencia de teléfonos se
            // representa con una lista vacía y no obliga al caso de uso a
            // preguntar por null en cada uso.
            request.Telefonos ?? new List<string>(),
            request.Direcciones ?? new List<string>());

        var resultado = await _crearCliente.EjecutarAsync(entrada, cancellationToken);

        // Devuelve 200 con cuerpo y no 204, porque la respuesta trae la
        // advertencia del CA2: si ya existe otro cliente con la misma razón
        // social, el alta se hace igual pero el usuario tiene que enterarse.
        return this.AResultadoHttp(resultado);
    }
}

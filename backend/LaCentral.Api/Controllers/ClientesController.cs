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
    private readonly ConsultarClientesUseCase _consultarClientes;
    private readonly ObtenerClienteDetalleUseCase _obtenerDetalle;
    private readonly ModificarClienteUseCase _modificarCliente;

    public ClientesController(
        CrearClienteUseCase crearCliente, 
        ConsultarClientesUseCase consultarClientes,
        ObtenerClienteDetalleUseCase obtenerDetalle,
        ModificarClienteUseCase modificarCliente)
    {
        _crearCliente = crearCliente;
        _consultarClientes = consultarClientes;
        _obtenerDetalle = obtenerDetalle;
        _modificarCliente = modificarCliente;
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
            request.CondicionFiscal,
            request.CondicionPago,
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

    /// <summary>
    /// Búsqueda de clientes por Razón Social (coincidencia difusa, puede devolver
    /// varios) o por CUIT/CUIL (coincidencia exacta, devuelve a lo sumo uno). HU-CLI-02.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(
        [FromQuery] string texto, [FromQuery] bool incluirInactivos, CancellationToken cancellationToken)
    {
        var resultado = await _consultarClientes.EjecutarAsync(texto, incluirInactivos, cancellationToken);

        return this.AResultadoHttp(resultado);
    }

    /// <summary>
    /// Detalle completo de un cliente, incluyendo todas sus direcciones y teléfonos. HU-CLI-02.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(int id, CancellationToken cancellationToken)
    {
        // Nota: Si usás [FromServices] directo en el método, te ahorrás inyectarlo en el constructor.
        // Queda a criterio de cómo lo vengan manejando.
        var resultado = await _obtenerDetalle.EjecutarAsync(id, cancellationToken);
        return this.AResultadoHttp(resultado);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Modificar(int id, [FromBody] DtosNucleo.ModificarClienteRequest request, CancellationToken ct)
    {
        // _modificarCliente debe inyectarse en el constructor del controller previamente
        var resultado = await _modificarCliente.EjecutarAsync(id, request, ct);
        return this.AResultadoHttp(resultado);
    }
}

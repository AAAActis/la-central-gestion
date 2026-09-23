using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.Proveedores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Proveedores.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/proveedores")]
// Mismo criterio que ClientesController: [Authorize] sin roles, cualquier
// puesto autenticado opera con proveedores.
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly CrearProveedorUseCase _crearProveedor;
    private readonly ConsultarProveedoresUseCase _consultarProveedores;

    public ProveedoresController(
        CrearProveedorUseCase crearProveedor,
        ConsultarProveedoresUseCase consultarProveedores)
    {
        _crearProveedor = crearProveedor;
        _consultarProveedores = consultarProveedores;
    }

    /// <summary>Alta de proveedor con sus teléfonos y direcciones. HU-PRO-01.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(
        CrearProveedorRequest request, CancellationToken cancellationToken)
    {
        var entrada = new DtosNucleo.CrearProveedorRequest(
            request.Codigo,
            request.RazonSocial,
            request.Cuit,
            request.UrlReferencia,
            // El núcleo espera listas, no null.
            request.Telefonos ?? new List<string>(),
            request.Direcciones ?? new List<string>());

        var resultado = await _crearProveedor.EjecutarAsync(entrada, cancellationToken);

        // Devuelve 200 con cuerpo y no 204: la respuesta trae la advertencia
        // de Razón Social repetida, igual que en Cliente.
        return this.AResultadoHttp(resultado);
    }

    /// <summary>
    /// Búsqueda de proveedores por Razón Social (coincidencia difusa, puede
    /// devolver varios) o por CUIT (coincidencia exacta, devuelve a lo sumo
    /// uno). HU-PRO-02.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Consultar(
        [FromQuery] string texto, [FromQuery] bool incluirInactivos, CancellationToken cancellationToken)
    {
        var resultado = await _consultarProveedores.EjecutarAsync(texto, incluirInactivos, cancellationToken);

        return this.AResultadoHttp(resultado);
    }
}

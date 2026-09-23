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
    private readonly ModificarProveedorUseCase _modificarProveedor;

    public ProveedoresController(
        CrearProveedorUseCase crearProveedor,
        ConsultarProveedoresUseCase consultarProveedores,
        ModificarProveedorUseCase modificarProveedor)
    {
        _crearProveedor = crearProveedor;
        _consultarProveedores = consultarProveedores;
        _modificarProveedor = modificarProveedor;
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

    /// <summary>
    /// Modificación de un proveedor activo, incluidos teléfonos, direcciones y URL. HU-PRO-03.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Modificar(
        int id, [FromBody] DtosNucleo.ModificarProveedorRequest request, CancellationToken ct)
    {
        var resultado = await _modificarProveedor.EjecutarAsync(id, request, ct);
        return this.AResultadoHttp(resultado);
    }

    /// <summary>
    /// Baja lógica de un proveedor exigiendo confirmación explícita (CUIT o Código) y motivo. HU-PRO-04.
    /// </summary>
    [HttpPost("{id}/baja")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DarDeBaja(
        int id,
        [FromBody] BajaProveedorRequest request,
        [FromServices] DarDeBajaProveedorUseCase useCase,
        CancellationToken ct)
    {
        var entrada = new DtosNucleo.DarDeBajaProveedorRequest(request.MotivoBaja, request.CuitReescrito);
        var resultado = await useCase.EjecutarAsync(id, entrada, ct);
        return this.AResultadoHttp(resultado);
    }

    /// <summary>
    /// Reactivación de un proveedor inactivo conservando su historial de baja. HU-PRO-05.
    /// </summary>
    [HttpPost("{id}/reactivacion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivar(
        int id,
        [FromServices] ReactivarProveedorUseCase useCase,
        CancellationToken ct)
    {
        var resultado = await useCase.EjecutarAsync(id, ct);
        return this.AResultadoHttp(resultado);
    }
}

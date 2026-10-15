using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.Articulos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Articulos.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/articulos")]
// Alta y modificación de datos comunes: ADMINISTRADOR y OPERADOR.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class ArticulosController : ControllerBase
{
    private readonly CrearArticuloUseCase _crearArticulo;

    public ArticulosController(CrearArticuloUseCase crearArticulo)
    {
        _crearArticulo = crearArticulo;
    }

    /// <summary>
    /// Alta de artículo. HU-ART-01. El código interno lo genera el sistema; costo y
    /// precios nacen en null. 201 con el Id, el código interno y Location.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Crear(CrearArticuloRequest request, CancellationToken cancellationToken)
    {
        var entrada = new DtosNucleo.CrearArticuloRequest(
            request.Nombre,
            request.UbicacionDeposito,
            (request.CodigosProveedor ?? new List<CodigoProveedorRequest>())
                .Select(c => new DtosNucleo.CodigoProveedorRequest(c.ProveedorId, c.Codigo)).ToList());

        var resultado = await _crearArticulo.EjecutarAsync(entrada, cancellationToken);

        // GET /api/articulos/{id} es de HU-ART-02 (Fase 4): la ruta ya es la definitiva.
        return this.ACreadoHttp(resultado, creado => $"/api/articulos/{creado.Id}");
    }
}

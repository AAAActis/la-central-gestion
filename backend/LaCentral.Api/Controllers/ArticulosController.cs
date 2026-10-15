using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.Articulos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.Articulos.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/articulos")]
// Consulta, alta y modificación de datos comunes: ADMINISTRADOR y OPERADOR.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class ArticulosController : ControllerBase
{
    private readonly CrearArticuloUseCase _crearArticulo;
    private readonly ConsultarArticulosUseCase _consultarArticulos;
    private readonly ObtenerArticuloDetalleUseCase _obtenerDetalle;

    public ArticulosController(
        CrearArticuloUseCase crearArticulo,
        ConsultarArticulosUseCase consultarArticulos,
        ObtenerArticuloDetalleUseCase obtenerDetalle)
    {
        _crearArticulo = crearArticulo;
        _consultarArticulos = consultarArticulos;
        _obtenerDetalle = obtenerDetalle;
    }

    /// <summary>
    /// Búsqueda por código interno, código de proveedor o nombre; alternativas separadas
    /// por *. HU-ART-02. Texto vacío → 400. Sin resultados → 200 con lista vacía, nunca 404.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Consultar(
        [FromQuery] string? texto,
        [FromQuery] bool incluirInactivos = false,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanoPagina = 20,
        CancellationToken cancellationToken = default)
    {
        // Texto ausente es lo mismo que vacío: lo rechaza el caso de uso con su mensaje.
        var consulta = new DtosNucleo.ConsultaArticulos(texto ?? string.Empty, incluirInactivos, pagina, tamanoPagina);
        var resultado = await _consultarArticulos.EjecutarAsync(consulta, cancellationToken);
        return this.AResultadoHttp(resultado);
    }

    /// <summary>Ficha: las seis modalidades, cada una con precio o con la razón de su ausencia. HU-ART-02.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerDetalle(int id, CancellationToken cancellationToken)
    {
        var resultado = await _obtenerDetalle.EjecutarAsync(id, cancellationToken);
        return this.AResultadoHttp(resultado);
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

        return this.ACreadoHttp(resultado, creado => $"/api/articulos/{creado.Id}");
    }
}

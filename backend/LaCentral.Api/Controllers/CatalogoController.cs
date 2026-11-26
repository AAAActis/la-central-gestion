using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.Catalogo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/catalogo")]
// Consulta de datos comunes: ADMINISTRADOR y OPERADOR.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class CatalogoController : ControllerBase
{
    private readonly BuscarCandidatosUseCase _buscarCandidatos;
    private readonly VincularFilaCatalogoUseCase _vincular;
    private readonly CrearYVincularArticuloUseCase _crearYVincular;

    public CatalogoController(BuscarCandidatosUseCase buscarCandidatos, VincularFilaCatalogoUseCase vincular,
        CrearYVincularArticuloUseCase crearYVincular)
        => (_buscarCandidatos, _vincular, _crearYVincular) = (buscarCandidatos, vincular, crearYVincular);

    /// <summary>
    /// 053 · Candidatos de vinculación: exactas y, aparte, aproximadas por parecido (pg_trgm). Con
    /// <c>articuloId</c>, también por el nombre del artículo y de sus filas ya vinculadas (CA5).
    /// Sin texto ni artículo → 400. Sin candidatos → 200 con el mensaje del CA6.
    /// </summary>
    [HttpGet("candidatos")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Candidatos([FromQuery] string? texto, [FromQuery] int? articuloId,
        CancellationToken cancellationToken = default)
        => this.AResultadoHttp(await _buscarCandidatos.EjecutarAsync(texto, articuloId, cancellationToken));

    /// <summary>
    /// 054a · Vincula la fila de catálogo con un artículo propio (CA1). CA10: OPERADOR también. Código
    /// ya vinculado a otro artículo → 409 con cuál (CA6). Fila vencida, proveedor o artículo de baja → 400.
    /// </summary>
    [HttpPost("filas/{filaId:int}/vinculacion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Vincular(int filaId, [FromBody] VincularFilaRequest request,
        CancellationToken cancellationToken = default)
        => this.AResultadoHttp(await _vincular.EjecutarAsync(filaId, request.ArticuloId, cancellationToken));

    /// <summary>054a CA5 · Da de alta el artículo desde la fila y lo deja vinculado, en una sola operación.</summary>
    [HttpPost("filas/{filaId:int}/alta-vinculada")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AltaVinculada(int filaId, [FromBody] AltaVinculadaRequest request,
        CancellationToken cancellationToken = default)
        => this.AResultadoHttp(await _crearYVincular.EjecutarAsync(filaId, request.Nombre, cancellationToken));
}

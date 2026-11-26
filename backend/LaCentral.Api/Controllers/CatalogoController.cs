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

    public CatalogoController(BuscarCandidatosUseCase buscarCandidatos) => _buscarCandidatos = buscarCandidatos;

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
}

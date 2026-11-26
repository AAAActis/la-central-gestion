using LaCentral.Api.Middleware;
using LaCentral.UseCases.Comparador;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/articulos")]
// Consulta de datos comunes: ADMINISTRADOR y OPERADOR.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class ComparadorController : ControllerBase
{
    private readonly ObtenerComparadorUseCase _obtenerComparador;
    public ComparadorController(ObtenerComparadorUseCase obtenerComparador) => _obtenerComparador = obtenerComparador;

    /// <summary>030 · Comparador de precios del artículo, del más barato al más caro por precio neto (D-S7-09).</summary>
    [HttpGet("{id:int}/comparador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken = default)
        => this.AResultadoHttp(await _obtenerComparador.EjecutarAsync(id, cancellationToken));
}

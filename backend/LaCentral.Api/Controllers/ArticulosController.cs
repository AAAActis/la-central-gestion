using LaCentral.Api.Dtos;
using LaCentral.UseCases.Articulos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>Alta de artículo. HU-ART-01.</summary>
    [HttpPost]
    public Task<IActionResult> Crear(CrearArticuloRequest request, CancellationToken cancellationToken)
        => Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status501NotImplemented));
}

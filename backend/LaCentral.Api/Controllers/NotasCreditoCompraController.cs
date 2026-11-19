using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.NotasCreditoCompra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.NotasCreditoCompra.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/notas-credito-compra")]
// CA-009: ADMINISTRADOR y OPERADOR, como la factura de compra. El usuario sale de la sesión.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class NotasCreditoCompraController : ControllerBase
{
    private readonly RegistrarNotaCreditoCompraUseCase _registrar;
    private readonly AnularNotaCreditoCompraUseCase _anular;
    private readonly InterpretarNotaCreditoPdfUseCase _interpretar;
    private readonly ConsultarNotasCreditoCompraUseCase _consultar;

    // Como la factura (029): una nota pesa decenas de KB; 10 MB deja margen sin aceptar cualquier cosa.
    private const long TamanoMaximoPdf = 10 * 1024 * 1024;

    public NotasCreditoCompraController(RegistrarNotaCreditoCompraUseCase registrar, AnularNotaCreditoCompraUseCase anular,
        InterpretarNotaCreditoPdfUseCase interpretar, ConsultarNotasCreditoCompraUseCase consultar)
        => (_registrar, _anular, _interpretar, _consultar) = (registrar, anular, interpretar, consultar);

    /// <summary>
    /// CA-012. Listado por proveedor y rango de emisión, todos opcionales; las anuladas vienen
    /// marcadas. 200 con la página (vacía si no hay); 400 si la página o el rango no son válidos.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Consultar(
        [FromQuery] int? proveedorId, [FromQuery] DateOnly? emisionDesde, [FromQuery] DateOnly? emisionHasta,
        CancellationToken ct, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
        => this.AResultadoHttp(await _consultar.EjecutarAsync(
            new DtosNucleo.FiltroNotasCreditoCompra(proveedorId, emisionDesde, emisionHasta), pagina, tamanoPagina, ct));

    /// <summary>
    /// CA-010. Interpreta el PDF del proveedor (campo de formulario «archivo») y devuelve la nota
    /// propuesta para revisar en la carga manual, sin guardar nada. 200; 400 si no es un PDF con
    /// texto o su formato no se reconoce: se pide la carga manual.
    /// </summary>
    [HttpPost("pdf")]
    [RequestSizeLimit(TamanoMaximoPdf)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> InterpretarPdf(IFormFile? archivo, CancellationToken ct)
    {
        using var contenido = new MemoryStream();
        if (archivo is not null)
            await archivo.CopyToAsync(contenido, ct);
        return this.AResultadoHttp(await _interpretar.EjecutarAsync(contenido.ToArray(), ct));
    }

    /// <summary>
    /// HU-FCO-07. Cabecera, facturas y, en una devolución, la resta del stock de cada origen, en
    /// una sola operación. 201 con el Id; 400 si no cumple las reglas; 409 si el número ya está
    /// vigente para el proveedor o una factura se anuló entre medio.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarNotaCreditoCompraRequest request, CancellationToken ct)
        => this.ACreadoHttp(await _registrar.EjecutarAsync(request.ANucleo(), ct), nota => $"/api/notas-credito-compra/{nota.Id}");

    /// <summary>CA-007: neto, 21 % de IVA, percepciones y total, sin guardar nada.</summary>
    [HttpPost("calculo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Calcular(RegistrarNotaCreditoCompraRequest request)
        => this.AResultadoHttp(_registrar.CalcularTotales(request.ANucleo()));

    /// <summary>
    /// CA-012 (D-S6-23). Como la factura (028): anula quien está en la sesión y la autoriza otro
    /// ADMINISTRADOR con su contraseña. 204; 400 sin motivo o sin autorización válida; 404 si no
    /// existe; 409 si ya estaba anulada.
    /// </summary>
    [HttpPost("{id}/anulacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(int id, [FromBody] DtosNucleo.AnularNotaCreditoCompraRequest request, CancellationToken ct)
        => this.AResultadoHttp(await _anular.EjecutarAsync(id, request, ct));
}

using LaCentral.Api.Dtos;
using LaCentral.Api.Middleware;
using LaCentral.UseCases.FacturasCompra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DtosNucleo = LaCentral.UseCases.FacturasCompra.Dtos;

namespace LaCentral.Api.Controllers;

[ApiController]
[Route("api/facturas-compra")]
// Alta de datos comunes: ADMINISTRADOR y OPERADOR. El usuario sale de la sesión.
[Authorize(Roles = "ADMINISTRADOR,OPERADOR")]
public class FacturasCompraController : ControllerBase
{
    private readonly RegistrarFacturaCompraUseCase _registrar;
    private readonly ConsultarFacturasCompraUseCase _consultar;
    private readonly AnularFacturaCompraUseCase _anular;
    private readonly InterpretarFacturaPdfUseCase _interpretar;

    // Una factura de un par de páginas pesa decenas de KB; 10 MB deja margen sin aceptar cualquier cosa.
    private const long TamanoMaximoPdf = 10 * 1024 * 1024;

    public FacturasCompraController(RegistrarFacturaCompraUseCase registrar, ConsultarFacturasCompraUseCase consultar,
        AnularFacturaCompraUseCase anular, InterpretarFacturaPdfUseCase interpretar)
        => (_registrar, _consultar, _anular, _interpretar) = (registrar, consultar, anular, interpretar);

    /// <summary>
    /// HU-FCO-05 (029). Interpreta el PDF del proveedor (campo de formulario «archivo») y
    /// devuelve la factura propuesta para revisar, sin guardar nada. 200; 400 si no es un PDF
    /// con texto o su formato no se reconoce: se pide la carga manual.
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
    /// HU-FCO-02 (026). Consulta por proveedor, número y rangos de emisión y de vencimiento;
    /// todos opcionales. Las anuladas vienen marcadas. 200 con la página (vacía si no hay).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Consultar(
        [FromQuery] int? proveedorId, [FromQuery] string? numero,
        [FromQuery] DateOnly? emisionDesde, [FromQuery] DateOnly? emisionHasta,
        [FromQuery] DateOnly? vencimientoDesde, [FromQuery] DateOnly? vencimientoHasta,
        CancellationToken ct, [FromQuery] int pagina = 1, [FromQuery] int tamanoPagina = 20)
        => this.AResultadoHttp(await _consultar.EjecutarAsync(new DtosNucleo.FiltroFacturasCompra(
            proveedorId, numero, emisionDesde, emisionHasta, vencimientoDesde, vencimientoHasta), pagina, tamanoPagina, ct));

    /// <summary>
    /// HU-FCO-04 (028). Anula quien está en la sesión; la autoriza otro usuario ADMINISTRADOR
    /// con su usuario y contraseña (doble certificación). 204; 400 sin motivo o sin
    /// autorización válida; 404 si no existe; 409 si ya estaba anulada.
    /// </summary>
    [HttpPost("{id}/anulacion")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Anular(int id, [FromBody] DtosNucleo.AnularFacturaCompraRequest request, CancellationToken ct)
        => this.AResultadoHttp(await _anular.EjecutarAsync(id, request, ct));

    /// <summary>
    /// Registro manual de factura de compra. HU-FCO-01. En una sola operación: factura,
    /// stock del destino de cada línea, costo, último proveedor e historial. 201 con el Id;
    /// 409 si el número ya está activo para el proveedor, informando con qué factura choca.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Registrar(RegistrarFacturaCompraRequest request, CancellationToken cancellationToken)
    {
        var resultado = await _registrar.EjecutarAsync(ANucleo(request), cancellationToken);

        return this.ACreadoHttp(resultado, registrada => $"/api/facturas-compra/{registrada.Id}");
    }

    /// <summary>
    /// HU-FCO-06 CA-014 y CA-015: neto, IVA (21 % si el comprobante es A), percepciones y total
    /// de la factura, sin guardar nada. Mismas validaciones de datos que el registro.
    /// </summary>
    [HttpPost("calculo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Calcular(RegistrarFacturaCompraRequest request)
        => this.AResultadoHttp(_registrar.CalcularTotales(ANucleo(request)));

    private static DtosNucleo.RegistrarFacturaCompraRequest ANucleo(RegistrarFacturaCompraRequest request) => new(
        request.ProveedorId,
        request.Numero,
        request.FechaEmision!.Value,
        request.FechaVencimiento,
        request.PercepcionIva,
        request.PercepcionIibb,
        request.PercepcionMunicipal,
        request.PercepcionGanancias,
        request.Lineas?.Select(l => new DtosNucleo.LineaFacturaCompraRequest(
            l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje,
            l.Descuento2Porcentaje, l.Descuento3Porcentaje)).ToList(),
        request.TipoComprobante,
        request.FechaImputacion,
        request.DesdePdf);
}

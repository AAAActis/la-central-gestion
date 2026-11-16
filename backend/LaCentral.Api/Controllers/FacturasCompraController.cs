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

    public FacturasCompraController(RegistrarFacturaCompraUseCase registrar) => _registrar = registrar;

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
        request.FechaImputacion);
}

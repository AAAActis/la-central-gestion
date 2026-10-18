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
        var entrada = new DtosNucleo.RegistrarFacturaCompraRequest(
            request.ProveedorId,
            request.Numero,
            request.FechaEmision!.Value,
            request.FechaVencimiento,
            request.PercepcionIva,
            request.PercepcionIibb,
            request.PercepcionMunicipal,
            request.PercepcionGanancias,
            request.Lineas?.Select(l => new DtosNucleo.LineaFacturaCompraRequest(
                l.ArticuloId, l.SucursalId, l.Cantidad, l.PrecioUnitario, l.DescuentoPorcentaje)).ToList());

        var resultado = await _registrar.EjecutarAsync(entrada, cancellationToken);

        return this.ACreadoHttp(resultado, registrada => $"/api/facturas-compra/{registrada.Id}");
    }
}

using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-02 (026) · Consulta de facturas de compra.</summary>
public class ConsultarFacturasCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;

    public ConsultarFacturasCompraUseCase(IFacturaCompraRepositorio facturas) => _facturas = facturas;

    public Task<Result<PaginaDto<FacturaCompraResumen>>> EjecutarAsync(
        FiltroFacturasCompra filtro, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
        => Task.FromResult(Result<PaginaDto<FacturaCompraResumen>>.Success(
            new PaginaDto<FacturaCompraResumen>(new List<FacturaCompraResumen>(), 0, pagina, tamanoPagina)));
}

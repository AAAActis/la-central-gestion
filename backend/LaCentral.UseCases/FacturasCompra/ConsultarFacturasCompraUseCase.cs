using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-02 (026) · Consulta de facturas de compra.</summary>
public class ConsultarFacturasCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;

    public ConsultarFacturasCompraUseCase(IFacturaCompraRepositorio facturas) => _facturas = facturas;

    public async Task<Result<PaginaDto<FacturaCompraResumen>>> EjecutarAsync(
        FiltroFacturasCompra filtro, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanoPagina > int.MaxValue)
            return Fallo("La página o su tamaño no son válidos.");
        if (filtro.EmisionDesde > filtro.EmisionHasta)
            return Fallo("El rango de emisión empieza después de terminar.");
        if (filtro.VencimientoDesde > filtro.VencimientoHasta)
            return Fallo("El rango de vencimiento empieza después de terminar.");

        var numero = string.IsNullOrWhiteSpace(filtro.Numero) ? null : filtro.Numero.Trim();
        var (items, total) = await _facturas.BuscarAsync(filtro with { Numero = numero }, pagina, tamanoPagina, ct);
        return Result<PaginaDto<FacturaCompraResumen>>.Success(new PaginaDto<FacturaCompraResumen>(items, total, pagina, tamanoPagina));
    }

    private static Result<PaginaDto<FacturaCompraResumen>> Fallo(string mensaje)
        => Result<PaginaDto<FacturaCompraResumen>>.Failure(TipoError.Invalido, mensaje);
}

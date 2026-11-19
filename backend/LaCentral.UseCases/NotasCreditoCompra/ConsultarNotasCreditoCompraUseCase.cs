using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-012 · Listado de notas de crédito de proveedor, como la consulta de la 026.</summary>
public class ConsultarNotasCreditoCompraUseCase
{
    private readonly INotaCreditoCompraRepositorio _notas;

    public ConsultarNotasCreditoCompraUseCase(INotaCreditoCompraRepositorio notas) => _notas = notas;

    public async Task<Result<PaginaDto<NotaCreditoCompraResumen>>> EjecutarAsync(
        FiltroNotasCreditoCompra filtro, int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
    {
        if (pagina < 1 || tamanoPagina is < 1 or > 100 || (long)(pagina - 1) * tamanoPagina > int.MaxValue)
            return Fallo("La página o su tamaño no son válidos.");
        if (filtro.EmisionDesde > filtro.EmisionHasta)
            return Fallo("El rango de emisión empieza después de terminar.");

        var (items, total) = await _notas.BuscarAsync(filtro, pagina, tamanoPagina, ct);
        return Result<PaginaDto<NotaCreditoCompraResumen>>.Success(new PaginaDto<NotaCreditoCompraResumen>(items, total, pagina, tamanoPagina));
    }

    private static Result<PaginaDto<NotaCreditoCompraResumen>> Fallo(string mensaje)
        => Result<PaginaDto<NotaCreditoCompraResumen>>.Failure(TipoError.Invalido, mensaje);
}

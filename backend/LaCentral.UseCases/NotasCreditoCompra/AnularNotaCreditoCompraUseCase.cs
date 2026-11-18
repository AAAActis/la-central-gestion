using LaCentral.UseCases.Comun;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>HU-FCO-07 (057) CA-012 · Anulación de la nota de crédito, como la de la factura (028, D-S6-23).</summary>
public class AnularNotaCreditoCompraUseCase
{
    private readonly INotaCreditoCompraRepositorio _notas;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHash _hash;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public AnularNotaCreditoCompraUseCase(INotaCreditoCompraRepositorio notas, IUsuarioRepositorio usuarios,
        IServicioHash hash, IUnidadDeTrabajo unidadDeTrabajo, IContextoUsuario contexto)
        => (_notas, _usuarios, _hash, _unidadDeTrabajo, _contexto) = (notas, usuarios, hash, unidadDeTrabajo, contexto);

    public Task<Result> EjecutarAsync(int notaId, AnularNotaCreditoCompraRequest request, CancellationToken ct = default)
        => Task.FromResult(Result.Success());
}

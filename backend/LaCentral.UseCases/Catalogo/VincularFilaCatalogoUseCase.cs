using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Catalogo;

/// <summary>054a · HU-CMP-07: declarar que una fila del catálogo de un proveedor es un artículo propio.</summary>
public class VincularFilaCatalogoUseCase
{
    private readonly ICatalogoVinculacionRepositorio _catalogo;
    private readonly IContextoUsuario _contexto;

    public VincularFilaCatalogoUseCase(ICatalogoVinculacionRepositorio catalogo, IContextoUsuario contexto)
        => (_catalogo, _contexto) = (catalogo, contexto);

    public Task<Result<VinculacionDto>> EjecutarAsync(int filaId, int articuloId, CancellationToken ct = default)
        => throw new NotImplementedException();
}

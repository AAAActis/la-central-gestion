using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Catalogo;

/// <summary>054a CA5 · Dar de alta el artículo desde la fila de catálogo y dejarlo vinculado, en una sola operación.</summary>
public class CrearYVincularArticuloUseCase
{
    private readonly CrearArticuloUseCase _crear;
    private readonly VincularFilaCatalogoUseCase _vincular;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public CrearYVincularArticuloUseCase(CrearArticuloUseCase crear, VincularFilaCatalogoUseCase vincular, IUnidadDeTrabajo unidadDeTrabajo)
        => (_crear, _vincular, _unidadDeTrabajo) = (crear, vincular, unidadDeTrabajo);

    public Task<Result<AltaVinculadaDto>> EjecutarAsync(int filaId, string nombre, CancellationToken ct = default)
        => throw new NotImplementedException();
}

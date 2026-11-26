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

    /// <summary>
    /// El alta abre su propia unidad de trabajo, que acá queda anidada: si la vinculación falla
    /// (CA6, fila vencida, proveedor de baja), la exterior descarta también el artículo.
    /// </summary>
    public async Task<Result<AltaVinculadaDto>> EjecutarAsync(int filaId, string nombre, CancellationToken ct = default)
    {
        AltaVinculadaDto? alta = null;
        var resultado = await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            var creado = await _crear.EjecutarAsync(new(nombre, []), token);
            if (creado.IsFailure) return creado;
            var vinculado = await _vincular.EjecutarAsync(filaId, creado.Value.Id, token);
            if (vinculado.IsFailure) return vinculado;
            alta = new(creado.Value.Id, creado.Value.CodigoInterno, vinculado.Value.ProveedorId, vinculado.Value.Codigo);
            return Result.Success();
        }, ct);
        return resultado.IsSuccess
            ? Result<AltaVinculadaDto>.Success(alta!)
            : Result<AltaVinculadaDto>.Failure(resultado.Tipo, resultado.Error);
    }
}

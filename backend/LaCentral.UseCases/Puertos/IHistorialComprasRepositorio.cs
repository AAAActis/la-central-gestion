using LaCentral.UseCases.Articulos.Dtos;

namespace LaCentral.UseCases.Puertos;

/// <summary>016 · lectura del historial de compras. Solo lectura: no hay escritura manual (CA-005).</summary>
public interface IHistorialComprasRepositorio
{
    /// <summary>Del más reciente al más antiguo. Null si el artículo no existe; vacía si existe y nunca se compró.</summary>
    Task<IReadOnlyList<RegistroHistorialCompraDto>?> HistorialComprasAsync(int articuloId, CancellationToken ct = default);
}

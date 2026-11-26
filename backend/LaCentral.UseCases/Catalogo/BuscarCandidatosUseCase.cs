using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Catalogo;

/// <summary>053 · HU-CMP-06: candidatos de vinculación entre los catálogos de todos los proveedores.</summary>
public class BuscarCandidatosUseCase
{
    private readonly ICatalogoCandidatosRepositorio _catalogo;
    private readonly OpcionesCandidatos _opciones;

    public BuscarCandidatosUseCase(ICatalogoCandidatosRepositorio catalogo, OpcionesCandidatos opciones)
    {
        _catalogo = catalogo;
        _opciones = opciones;
    }

    /// <summary>
    /// Con texto, busca por él; con artículo (CA5), además por su nombre y por el de sus filas ya
    /// vinculadas. Una fila exacta no se repite entre las aproximadas (CA2).
    /// </summary>
    public Task<Result<ResultadoCandidatosDto>> EjecutarAsync(string? texto, int? articuloId, CancellationToken ct = default)
        => Task.FromResult(Result<ResultadoCandidatosDto>.Success(new(texto ?? string.Empty, [], [], null)));   // esqueleto: rojo
}

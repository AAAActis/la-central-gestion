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
        if (opciones.UmbralParecido is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(opciones), "El umbral de parecido va entre 0 (excluido) y 1.");
        if (opciones.MaximoAproximadas < 1)
            throw new ArgumentOutOfRangeException(nameof(opciones), "El máximo de aproximadas tiene que ser al menos 1.");
        _catalogo = catalogo;
        _opciones = opciones;
    }

    /// <summary>
    /// Con texto, busca por él; con artículo (CA5), además por su nombre y por el de sus filas ya
    /// vinculadas. Una fila exacta no se repite entre las aproximadas (CA2).
    /// </summary>
    public async Task<Result<ResultadoCandidatosDto>> EjecutarAsync(string? texto, int? articuloId, CancellationToken ct = default)
    {
        texto = (texto ?? string.Empty).Trim();
        if (texto.Length == 0 && articuloId is null)
            return Result<ResultadoCandidatosDto>.Failure(TipoError.Invalido, "Ingresá un texto o elegí un artículo para buscar candidatos.");

        var textos = new List<string>();
        if (texto.Length > 0) textos.Add(texto);
        if (articuloId is int id)
        {
            var delArticulo = await _catalogo.TextosDeArticuloAsync(id, ct);
            if (delArticulo is null)
                return Result<ResultadoCandidatosDto>.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
            textos.AddRange(delArticulo.Select(t => t.Trim()).Where(t => t.Length > 0));
        }
        textos = textos.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var clave = NormalizacionCatalogo.Clave(texto);
        var exactas = clave.Length > 0 ? await _catalogo.BuscarExactasAsync(clave, ct) : [];
        var ids = exactas.Select(e => e.Id).ToHashSet();
        var aproximadas = (await _catalogo.BuscarAproximadasAsync(textos, _opciones.UmbralParecido,
                _opciones.MaximoAproximadas + ids.Count, ct))
            .Where(a => !ids.Contains(a.Id)).Take(_opciones.MaximoAproximadas).ToList();

        var buscado = texto.Length > 0 ? texto : textos.FirstOrDefault() ?? string.Empty;
        var mensaje = exactas.Count == 0 && aproximadas.Count == 0 ? $"No hay candidatos para «{buscado}»." : null;
        return Result<ResultadoCandidatosDto>.Success(new(buscado, exactas, aproximadas, mensaje));
    }
}

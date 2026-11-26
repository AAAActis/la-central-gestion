namespace LaCentral.UseCases.Catalogo;

/// <summary>El artículo propio al que ya está vinculada una fila de catálogo (053 CA4).</summary>
public sealed record ArticuloVinculadoDto(int Id, string CodigoInterno, string Nombre);

/// <summary>
/// Una fila del catálogo de un proveedor. <c>Parecido</c> es la similitud pg_trgm (0 a 1) en las
/// aproximadas, y null en las exactas.
/// </summary>
public sealed record CandidatoCatalogoDto(
    int Id, int ProveedorId, string Proveedor, string Codigo, string Nombre,
    string? NumeroOriginal, string? Marca, double? Parecido, ArticuloVinculadoDto? Vinculado);

/// <summary>
/// 053 CA1: primero las exactas y, aparte, las aproximadas de mayor a menor parecido.
/// CA6: sin ninguna de las dos, <c>Mensaje</c> dice que no hay candidatos para el texto buscado.
/// </summary>
public sealed record ResultadoCandidatosDto(
    string Texto, IReadOnlyList<CandidatoCatalogoDto> Exactas, IReadOnlyList<CandidatoCatalogoDto> Aproximadas, string? Mensaje);

/// <summary>D-S7-11: el umbral es configurable; 0,3 es el de pg_trgm por defecto. La C6 sigue abierta.</summary>
public sealed record OpcionesCandidatos(double UmbralParecido = 0.3, int MaximoAproximadas = 50);

/// <summary>Lo que la 054 necesita de una fila de catálogo para vincularla.</summary>
public sealed record FilaParaVincularDto(int Id, int ProveedorId, string Codigo, bool Vencido, bool ProveedorActivo);

/// <summary>054 · El resultado de vincular: el artículo y el código que le quedó asociado.</summary>
public sealed record VinculacionDto(int ArticuloId, int ProveedorId, string Codigo);

/// <summary>054a CA5 · El artículo dado de alta desde la fila y el código que le quedó vinculado.</summary>
public sealed record AltaVinculadaDto(int ArticuloId, string CodigoInterno, int ProveedorId, string Codigo);

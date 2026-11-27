namespace LaCentral.UseCases.Comparador;

/// <summary>
/// Una fila de catálogo vinculada al artículo, tal como la lee el repositorio. <c>UltimaCorridaFallida</c>:
/// la última corrida terminada del scrape de ese proveedor quedó en FAILED (050 CA-003, 031 CA-003).
/// </summary>
public sealed record FilaCatalogoVinculada(
    int ProveedorId, string Proveedor, string? UrlReferencia, string Codigo, string Nombre, string? Marca,
    decimal? PrecioNeto, decimal? PrecioLista, string? EstadoStock, decimal? Stock, string? UrlImagen, DateTime UltimaLectura,
    bool UltimaCorridaFallida = false);

/// <summary>Lo que el comparador necesita de la base: las filas vinculadas y el último proveedor comprado (032 CA-003).</summary>
public sealed record DatosComparador(int? UltimoProveedorId, IReadOnlyList<FilaCatalogoVinculada> Filas);

/// <summary>
/// D-S7-09: una fila por código vinculado. Se compara el precio neto (costo sin IVA); el de lista se
/// muestra y no se compara. <c>DiferenciaConMasBarato</c> es 032 CA-002; null si la fila no tiene neto.
/// <c>UltimaConsultaFallida</c> (050 CA-003): el precio es el último conocido, del <c>UltimaLectura</c>.
/// </summary>
public sealed record FilaComparadorDto(
    int ProveedorId, string Proveedor, string? UrlReferencia, string Codigo, string Nombre, string? Marca,
    decimal? PrecioNeto, decimal? PrecioLista, decimal? DiferenciaConMasBarato,
    string? EstadoStock, decimal? Stock, string? UrlImagen, DateTime UltimaLectura, bool UltimoComprado,
    bool UltimaConsultaFallida);

/// <summary>054 CA4 (D-S7-09): por proveedor, el MÁXIMO entre sus filas, nunca la suma.</summary>
public sealed record ExistenciaProveedorDto(int ProveedorId, string? EstadoStock, decimal? Stock);

/// <summary>El comparador de un artículo. <c>Mensaje</c> cuando no tiene ninguna fila vinculada (030 CA-004).</summary>
public sealed record ComparadorDto(
    int ArticuloId, IReadOnlyList<FilaComparadorDto> Filas, IReadOnlyList<ExistenciaProveedorDto> ExistenciaPorProveedor, string? Mensaje);

using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Articulos.Dtos;

public sealed record ConsultaArticulos(
    string Texto, bool IncluirInactivos = false, int Pagina = 1, int TamanoPagina = 20)
{
    /// <summary>Regla de negocio de la búsqueda: la aplica el caso de uso, no el repositorio.</summary>
    public Result Validar()
    {
        if (string.IsNullOrWhiteSpace(Texto) || Texto.Length > 500)
            return Result.Failure(TipoError.Invalido, "Ingresá una búsqueda de 1 a 500 caracteres.");
        var alternativas = NormalizacionArticulo.Alternativas(Texto);
        if (alternativas.Count == 0 || alternativas.Count > 10 ||
            alternativas.Any(a => NormalizacionArticulo.Palabras(a).Count == 0))
            return Result.Failure(TipoError.Invalido, "Ingresá entre 1 y 10 alternativas de búsqueda.");
        if (Pagina < 1 || TamanoPagina is < 1 or > 100 ||
            (long)(Pagina - 1) * TamanoPagina > int.MaxValue)
            return Result.Failure(TipoError.Invalido, "La página o su tamaño no son válidos.");
        return Result.Success();
    }
}

public sealed record ArticuloResumenConsultaDto(int Id, string CodigoInterno, string Nombre, bool Activo);

public sealed record ConflictoCodigoArticuloDto(string ClaveNormalizada, IReadOnlyList<int> ArticuloIds);

/// <summary>Un código de proveedor tal como se cargó, para la lista de resultados.</summary>
public sealed record CodigoProveedorResumenDto(int ProveedorId, string RazonSocial, string Codigo);

/// <summary>HU-ART-02: cada resultado lleva su Id y sus códigos de proveedor.</summary>
public sealed record ArticuloResultadoBusquedaDto(
    int Id, string CodigoInterno, string Nombre, bool Activo,
    IReadOnlyList<CodigoProveedorResumenDto> CodigosProveedor);

public sealed record PaginaArticulosDto(
    IReadOnlyList<ArticuloResultadoBusquedaDto> Articulos, int Total, int Pagina, int TamanoPagina,
    IReadOnlyList<ConflictoCodigoArticuloDto> Conflictos);

public sealed record CodigoProveedorConsultaDto(
    int ProveedorId, string RazonSocial, string CodigoOriginal, string ClaveNormalizada);

public sealed record StockSucursalConsultaDto(short SucursalId, string Sucursal, decimal Cantidad);

/// <summary>Fila de articulo_precio_modalidad tal como está guardada (H3: todo por valor).</summary>
public sealed record PrecioModalidadConsultaDto(
    Modalidad Modalidad, BasePrecio? Base, decimal? Porcentaje, decimal? Valor,
    bool CalculoManual, DateTime? FechaCalculo, int? UsuarioId);

public sealed record ArticuloDetalleConsultaDto(
    int Id, string CodigoInterno, string Nombre, bool Activo, decimal? CostoVigente,
    int? UltimoProveedorId, IReadOnlyList<CodigoProveedorConsultaDto> Codigos,
    IReadOnlyList<StockSucursalConsultaDto> Existencias,
    IReadOnlyList<PrecioModalidadConsultaDto> PreciosPorModalidad);

/// <summary>
/// Por qué una modalidad no tiene precio. Viaja con el dato: un precio ausente sin razón
/// se confunde con un cero, y un precio de tarjeta en cero nadie lo ve hasta que se vende.
/// </summary>
public enum RazonAusenciaPrecio
{
    SinConfiguracion,
    SinCosto,
    CasoEspecialNoFijado,
    TasaSinDefinir
}

/// <summary>Una modalidad en la ficha. RazonAusencia es null si y solo si hay precio.</summary>
public sealed record PrecioModalidadFichaDto(
    Modalidad Modalidad, BasePrecio? Base, decimal? Porcentaje, decimal? Precio,
    bool CalculoManual, DateTime? FechaCalculo, int? UsuarioId, RazonAusenciaPrecio? RazonAusencia);

/// <summary>HU-ART-02: la ficha devuelve SIEMPRE las seis modalidades.</summary>
public sealed record FichaArticuloDto(
    int Id, string CodigoInterno, string Nombre, bool Activo, decimal? CostoVigente,
    int? UltimoProveedorId, IReadOnlyList<CodigoProveedorConsultaDto> Codigos,
    IReadOnlyList<StockSucursalConsultaDto> Existencias,
    IReadOnlyList<PrecioModalidadFichaDto> Precios);

namespace LaCentral.UseCases.Articulos.Dtos;

public sealed record ConsultaArticulos(
    string Texto, bool IncluirInactivos = false, int Pagina = 1, int TamanoPagina = 20)
{
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Texto) || Texto.Length > 500)
            throw new ArgumentException("Ingresá una búsqueda de 1 a 500 caracteres.", nameof(Texto));
        var alternativas = NormalizacionArticulo.Alternativas(Texto);
        if (alternativas.Count == 0 || alternativas.Count > 10 ||
            alternativas.Any(a => NormalizacionArticulo.Palabras(a).Count == 0))
            throw new ArgumentException("Ingresá entre 1 y 10 alternativas de búsqueda.", nameof(Texto));
        if (Pagina < 1 || TamanoPagina is < 1 or > 100 ||
            (long)(Pagina - 1) * TamanoPagina > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(Pagina), "La página o su tamaño no son válidos.");
    }
}

public sealed record ArticuloResumenConsultaDto(int Id, string CodigoInterno, string Nombre, bool Activo);

public sealed record ConflictoCodigoArticuloDto(string ClaveNormalizada, IReadOnlyList<int> ArticuloIds);

public sealed record PaginaArticulosDto(
    IReadOnlyList<ArticuloResumenConsultaDto> Articulos, int Total, int Pagina, int TamanoPagina,
    IReadOnlyList<ConflictoCodigoArticuloDto> Conflictos);

public sealed record CodigoProveedorConsultaDto(
    int ProveedorId, string RazonSocial, string CodigoOriginal, string ClaveNormalizada);

public sealed record StockSucursalConsultaDto(short SucursalId, string Sucursal, decimal Cantidad);

public sealed record PrecioModalidadConsultaDto(string Modalidad, decimal? Valor);

public sealed record ArticuloDetalleConsultaDto(
    int Id, string CodigoInterno, string Nombre, bool Activo, decimal? CostoVigente,
    int? UltimoProveedorId, IReadOnlyList<CodigoProveedorConsultaDto> Codigos,
    IReadOnlyList<StockSucursalConsultaDto> Existencias,
    IReadOnlyList<PrecioModalidadConsultaDto>? PreciosPorModalidad);

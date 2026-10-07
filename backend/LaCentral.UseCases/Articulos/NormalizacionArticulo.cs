namespace LaCentral.UseCases.Articulos;

/// <summary>Reglas compartidas por la futura escritura y la consulta del catálogo.</summary>
public static class NormalizacionArticulo
{
    // Conserva guiones, ceros y acentos del código: no presupone equivalencias
    // de proveedores. Trim de espacios coincide con btrim(text) de PostgreSQL.
    public static string Codigo(string codigo)
    {
        ArgumentNullException.ThrowIfNull(codigo);
        return codigo.Trim(' ').ToUpperInvariant();
    }

    public static string NombreParaBusqueda(string nombre)
    {
        ArgumentNullException.ThrowIfNull(nombre);
        return string.Join(' ', nombre.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant()
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I')
            .Replace('Ó', 'O').Replace('Ú', 'U').Replace('Ü', 'U');
    }

    public static IReadOnlyList<string> Alternativas(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        return texto.Split('*', StringSplitOptions.RemoveEmptyEntries)
            .Select(Codigo).Where(a => a.Length > 0)
            .Distinct(StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<string> Palabras(string alternativa)
        => NombreParaBusqueda(alternativa).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal).ToArray();
}

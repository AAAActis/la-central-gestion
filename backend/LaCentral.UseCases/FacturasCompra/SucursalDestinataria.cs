using LaCentral.UseCases.Articulos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>
/// Fix E: a qué sucursal va la mercadería, según el texto del destinatario del PDF. Sin mayúsculas ni
/// acentos. Si el texto nombra las dos sucursales o ninguna, no se sugiere nada: lo elige el operador.
/// No hay números de cuenta: no tenemos esos datos.
/// </summary>
public static class SucursalDestinataria
{
    // Códigos de `sucursal.codigo`; el id se resuelve por código, no por un id fijo.
    private static readonly (string Codigo, string[] Nombres)[] Sucursales =
    [
        ("FR", ["FRAGUEIRO"]),
        ("SV", ["SAN VICENTE", "SAN JERONIMO"]),
    ];

    public static string? Reconocer(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return null;
        var normalizado = NormalizacionArticulo.NombreParaBusqueda(texto);
        var coinciden = Sucursales.Where(s => s.Nombres.Any(normalizado.Contains)).Select(s => s.Codigo).ToArray();
        return coinciden.Length == 1 ? coinciden[0] : null;
    }
}

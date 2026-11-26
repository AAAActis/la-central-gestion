using System.Text;

namespace LaCentral.UseCases.Catalogo;

/// <summary>
/// 053 CA2: «GN/100» y «gn 100» son la misma clave. Mayúsculas, sin acentos y solo letras y dígitos.
/// El repositorio aplica la misma regla en SQL (CatalogoCandidatosRepositorio.Normalizar).
/// </summary>
public static class NormalizacionCatalogo
{
    public static string Clave(string texto)
    {
        ArgumentNullException.ThrowIfNull(texto);
        var clave = new StringBuilder(texto.Length);
        foreach (var c in texto.ToUpperInvariant())
        {
            var letra = c switch
            {
                'Á' => 'A', 'É' => 'E', 'Í' => 'I', 'Ó' => 'O', 'Ú' => 'U', 'Ü' => 'U', 'Ñ' => 'N', _ => c,
            };
            if (letra is >= 'A' and <= 'Z' or >= '0' and <= '9') clave.Append(letra);
        }
        return clave.ToString();
    }
}

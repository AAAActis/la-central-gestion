using System.Text;

namespace LaCentral.UseCases.Catalogo;

/// <summary>
/// 053 CA2: «GN/100» y «gn 100» son la misma clave. Mayúsculas, sin acentos y solo letras y dígitos.
/// El repositorio aplica la misma regla en SQL (CatalogoCandidatosRepositorio.Normalizar).
/// </summary>
public static class NormalizacionCatalogo
{
    public static string Clave(string texto) => texto;   // esqueleto: rojo
}

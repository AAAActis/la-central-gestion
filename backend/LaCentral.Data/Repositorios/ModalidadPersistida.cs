using LaCentral.UseCases.Entidades;

namespace LaCentral.Data.Repositorios;

/// <summary>
/// D1: articulo_precio_modalidad.modalidad guarda el nombre EXACTO del miembro del enum.
/// La conversión vive en el mapeo del repositorio porque el modelo scaffoldeado declara
/// la columna como string y no se edita.
/// </summary>
internal static class ModalidadPersistida
{
    // Comparación exacta, sin Enum.Parse: Parse acepta "3", espacios alrededor y
    // (con ignoreCase) "lista". Ninguno de esos es un valor del esquema.
    private static readonly Dictionary<string, Modalidad> PorNombre =
        Enum.GetValues<Modalidad>().ToDictionary(m => m.ToString(), StringComparer.Ordinal);

    /// <summary>
    /// Un valor fuera de los seis es un dato corrupto, no un caso de negocio: falla en vez
    /// de devolver un default. Un enum con default silencioso es un precio en cero.
    /// </summary>
    public static Modalidad Leer(string valor) =>
        PorNombre.TryGetValue(valor, out var modalidad)
            ? modalidad
            : throw new InvalidOperationException(
                $"articulo_precio_modalidad.modalidad = '{valor}' no es una Modalidad válida " +
                $"({string.Join(", ", PorNombre.Keys)}).");
}

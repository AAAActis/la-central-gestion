using LaCentral.UseCases.Entidades;

namespace LaCentral.Data.Repositorios;

/// <summary>
/// D1: articulo_precio_modalidad guarda la modalidad y la base como el nombre EXACTO del
/// miembro del enum. La conversión vive en el mapeo del repositorio porque el modelo
/// scaffoldeado declara las columnas como string y no se edita.
/// </summary>
internal static class ModalidadPersistida
{
    // Comparación exacta, sin Enum.Parse: Parse acepta "3", espacios alrededor y
    // (con ignoreCase) "lista". Ninguno de esos es un valor del esquema.
    private static readonly Dictionary<string, Modalidad> ModalidadPorNombre = PorNombre<Modalidad>();
    private static readonly Dictionary<string, BasePrecio> BasePorNombre = PorNombre<BasePrecio>();

    /// <summary>
    /// Un valor fuera de los seis es un dato corrupto, no un caso de negocio: falla en vez
    /// de devolver un default. Un enum con default silencioso es un precio en cero.
    /// </summary>
    public static Modalidad Leer(string valor) => Convertir(ModalidadPorNombre, valor, "modalidad");

    /// <summary>NULL es "sin fórmula". Cualquier texto fuera de los dos nombres falla.</summary>
    public static BasePrecio? LeerBase(string? valor) =>
        valor is null ? null : Convertir(BasePorNombre, valor, "base");

    private static Dictionary<string, T> PorNombre<T>() where T : struct, Enum =>
        Enum.GetValues<T>().ToDictionary(m => m.ToString(), StringComparer.Ordinal);

    private static T Convertir<T>(Dictionary<string, T> porNombre, string valor, string columna) =>
        porNombre.TryGetValue(valor, out var convertido)
            ? convertido
            : throw new InvalidOperationException(
                $"articulo_precio_modalidad.{columna} = '{valor}' no es un {typeof(T).Name} válido " +
                $"({string.Join(", ", porNombre.Keys)}).");
}

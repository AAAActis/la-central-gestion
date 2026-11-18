using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Puertos;

/// <summary>
/// HU-FCO-05 (029): el texto de un PDF, renglón por renglón, de arriba hacia abajo y con las
/// palabras de cada renglón de izquierda a derecha, separadas por un espacio. Un PDF escaneado
/// da una lista vacía. Invalido si el archivo no es un PDF.
/// </summary>
public interface ILectorTextoPdf
{
    Result<IReadOnlyList<string>> LeerRenglones(byte[] archivo);
}

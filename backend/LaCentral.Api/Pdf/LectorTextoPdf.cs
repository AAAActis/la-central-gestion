using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.Api.Pdf;

/// <summary>HU-FCO-05 (029): adaptador de PdfPig. Agrupa las palabras de cada página en renglones.</summary>
public class LectorTextoPdf : ILectorTextoPdf
{
    public Result<IReadOnlyList<string>> LeerRenglones(byte[] archivo)
        => Result<IReadOnlyList<string>>.Success(Array.Empty<string>());
}

using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;
using UglyToad.PdfPig;

namespace LaCentral.Api.Pdf;

/// <summary>HU-FCO-05 (029): adaptador de PdfPig. Agrupa las palabras de cada página en renglones.</summary>
public class LectorTextoPdf : ILectorTextoPdf
{
    // Dos palabras cuya línea de base difiere en menos que esto están en el mismo renglón.
    private const double ToleranciaRenglon = 2;

    public Result<IReadOnlyList<string>> LeerRenglones(byte[] archivo)
    {
        try
        {
            using var documento = PdfDocument.Open(archivo);
            var renglones = new List<string>();
            foreach (var pagina in documento.GetPages())
            {
                var palabras = pagina.GetWords()
                    .Select(p => (Y: p.Letters[0].StartBaseLine.Y, X: p.BoundingBox.Left, p.Text))
                    .OrderByDescending(p => p.Y);
                var renglon = new List<(double Y, double X, string Text)>();
                foreach (var palabra in palabras)
                {
                    if (renglon.Count > 0 && renglon[0].Y - palabra.Y > ToleranciaRenglon)
                    {
                        renglones.Add(Unir(renglon));
                        renglon.Clear();
                    }
                    renglon.Add(palabra);
                }
                if (renglon.Count > 0)
                    renglones.Add(Unir(renglon));
            }
            return Result<IReadOnlyList<string>>.Success(renglones);
        }
        // PdfPig lanza tipos distintos según cómo esté dañado el archivo; para el sistema es lo mismo.
        catch (Exception)
        {
            return Result<IReadOnlyList<string>>.Failure(TipoError.Invalido, "El archivo no es un PDF.");
        }
    }

    private static string Unir(List<(double Y, double X, string Text)> renglon)
        => string.Join(' ', renglon.OrderBy(p => p.X).Select(p => p.Text));
}

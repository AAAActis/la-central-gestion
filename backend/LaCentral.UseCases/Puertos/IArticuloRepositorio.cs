using LaCentral.UseCases.Entidades;

namespace LaCentral.UseCases.Puertos;

public interface IArticuloRepositorio
{
    // Devuelven el Articulo completo (o un DTO ligero si prefieren) para poder 
    // leer el Nombre y armar el mensaje de error: "El código pertenece a: {articulo.Nombre}"
    Task<Articulo?> ObtenerPorCodigoInternoAsync(string codigoInterno, CancellationToken ct = default);
    Task<Articulo?> ObtenerPorCodigoAlternativoAsync(string codigoAlternativo, CancellationToken ct = default);
    
    Task<Articulo?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);
    
    // Búsqueda cruzada para HU-ART-02
    Task<IReadOnlyList<Articulo>> BuscarAsync(string texto, CancellationToken ct = default);

    Task AgregarAsync(Articulo articulo, CancellationToken ct = default);
    
    // Hallazgo 11: Retorna bool para confirmar que el registro existía y se pisó correctamente
    Task<bool> ActualizarAsync(Articulo articulo, CancellationToken ct = default);
}
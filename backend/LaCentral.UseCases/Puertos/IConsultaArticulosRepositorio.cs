using LaCentral.UseCases.Articulos.Dtos;

namespace LaCentral.UseCases.Puertos;

/// <summary>Lectura ART-02. No anticipa el agregado de escritura pendiente de revisión en #103.</summary>
public interface IConsultaArticulosRepositorio
{
    Task<PaginaArticulosDto> BuscarAsync(ConsultaArticulos consulta, CancellationToken ct = default);
    Task<ArticuloDetalleConsultaDto?> ObtenerDetallePorIdAsync(int id, CancellationToken ct = default);

    // Examina también inactivos: una baja lógica no libera una identidad.
    Task<IReadOnlyList<ArticuloResumenConsultaDto>> ObtenerConflictosCodigoAsync(
        string codigo, CancellationToken ct = default);
}

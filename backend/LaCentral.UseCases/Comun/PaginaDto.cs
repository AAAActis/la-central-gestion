namespace LaCentral.UseCases.Comun;

/// <summary>
/// #147 · una página de resultados, con la misma forma vacía o llena: el listado del frontend
/// dibuja su estado vacío sin casos especiales por endpoint.
/// </summary>
public sealed record PaginaDto<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina);

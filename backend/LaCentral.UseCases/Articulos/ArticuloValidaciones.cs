using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Articulos;

/// <summary>Validaciones del artículo, compartidas por el alta y la modificación.</summary>
public static class ArticuloValidaciones
{
    public static Result ValidarDatos(string nombre, string? ubicacionDeposito) => Result.Success();

    public static Result ValidarCodigos(IReadOnlyList<CodigoProveedorRequest> codigos) => Result.Success();
}

using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;

namespace LaCentral.UseCases.Articulos;

/// <summary>Validaciones del artículo, compartidas por el alta y la modificación.</summary>
public static class ArticuloValidaciones
{
    // Límites de articulo.nombre, articulo.ubicacion_deposito y articulo_codigo_alternativo.codigo.
    public const int LargoNombre = 200;
    public const int LargoUbicacion = 60;
    public const int LargoCodigo = 60;

    public static Result ValidarDatos(string nombre, string? ubicacionDeposito)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > LargoNombre)
            return Result.Failure(TipoError.Invalido, $"El nombre es obligatorio y no puede superar los {LargoNombre} caracteres.");

        if (ubicacionDeposito is not null && ubicacionDeposito.Trim().Length > LargoUbicacion)
            return Result.Failure(TipoError.Invalido, $"La ubicación en depósito no puede superar los {LargoUbicacion} caracteres.");

        return Result.Success();
    }

    /// <summary>
    /// Forma de cada código y las dos unicidades dentro de la misma petición: D2 (uno por
    /// proveedor) y la global (la misma clave normalizada, aunque sea de otro proveedor).
    /// </summary>
    public static Result ValidarCodigos(IReadOnlyList<CodigoProveedorRequest> codigos)
    {
        foreach (var codigo in codigos)
        {
            var clave = NormalizacionArticulo.Codigo(codigo.Codigo ?? string.Empty);
            if (clave.Length == 0 || clave.Length > LargoCodigo)
                return Result.Failure(TipoError.Invalido, $"Cada código de proveedor es obligatorio y no puede superar los {LargoCodigo} caracteres.");
            if (codigo.ProveedorId <= 0)
                return Result.Failure(TipoError.Invalido, "Cada código tiene que indicar su proveedor.");
        }

        var repetida = codigos.GroupBy(c => NormalizacionArticulo.Codigo(c.Codigo))
            .FirstOrDefault(g => g.Count() > 1);
        if (repetida is not null)
            return Result.Failure(TipoError.Conflicto, $"El código {repetida.Key} está más de una vez en el artículo.");

        return Result.Success();
    }
}

namespace LaCentral.UseCases.Entidades;

/// <summary>
/// D1: sobre qué valor se aplica el porcentaje de una modalidad.
/// OJO: CostoArticulo es el costo del artículo, NO la modalidad Costo. Comparten nombre.
/// </summary>
public enum BasePrecio
{
    CostoArticulo,
    PrecioLista
}

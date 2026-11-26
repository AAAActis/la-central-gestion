using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Catalogo;

/// <summary>054a · HU-CMP-07: declarar que una fila del catálogo de un proveedor es un artículo propio.</summary>
public class VincularFilaCatalogoUseCase
{
    private readonly ICatalogoVinculacionRepositorio _catalogo;
    private readonly IContextoUsuario _contexto;

    public VincularFilaCatalogoUseCase(ICatalogoVinculacionRepositorio catalogo, IContextoUsuario contexto)
        => (_catalogo, _contexto) = (catalogo, contexto);

    /// <summary>
    /// CA1: una sola fila de articulo_codigo_alternativo con el usuario de la sesión y la fecha.
    /// CA6: si el código ya es de otro artículo, informa cuál y no elige por el operador.
    /// </summary>
    public async Task<Result<VinculacionDto>> EjecutarAsync(int filaId, int articuloId, CancellationToken ct = default)
    {
        var fila = await _catalogo.ObtenerFilaAsync(filaId, ct);
        if (fila is null)
            return Result<VinculacionDto>.Failure(TipoError.NoEncontrado, "La fila de catálogo indicada no existe.");
        if (fila.Vencido)
            return Result<VinculacionDto>.Failure(TipoError.Invalido, "La fila de catálogo está vencida: el proveedor ya no la publica.");
        if (!fila.ProveedorActivo)
            return Result<VinculacionDto>.Failure(TipoError.Invalido, "El proveedor de la fila está dado de baja.");

        var activo = await _catalogo.ArticuloActivoAsync(articuloId, ct);
        if (activo is null)
            return Result<VinculacionDto>.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (activo == false)
            return Result<VinculacionDto>.Failure(TipoError.Invalido, "El artículo está dado de baja: reactivalo antes de vincularlo.");

        // D-S7-14: el código como lo guarda la ficha, así la vinculación automática por igualdad lo encuentra.
        var codigo = NormalizacionArticulo.Codigo(fila.Codigo);
        var vinculacion = new VinculacionDto(articuloId, fila.ProveedorId, codigo);

        var vinculado = await _catalogo.VinculadoAAsync(fila.ProveedorId, codigo, ct);
        if (vinculado?.Id == articuloId)
            return Result<VinculacionDto>.Success(vinculacion);
        if (vinculado is null && await _catalogo.VincularAsync(articuloId, fila.ProveedorId, codigo, _contexto.UsuarioId, ct))
            return Result<VinculacionDto>.Success(vinculacion);

        // Chocó antes o en la escritura (otro lo vinculó en el medio): se informa a cuál.
        vinculado ??= await _catalogo.VinculadoAAsync(fila.ProveedorId, codigo, ct);
        var cual = vinculado is null ? "otro artículo" : $"el artículo {vinculado.CodigoInterno} · {vinculado.Nombre}";
        return Result<VinculacionDto>.Failure(TipoError.Conflicto,
            $"El código {codigo} de este proveedor ya está vinculado a {cual}. Corregí una de las dos vinculaciones.");
    }
}

using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-02 · Ficha de artículo.</summary>
public class ObtenerArticuloDetalleUseCase
{
    private readonly IConsultaArticulosRepositorio _consulta;

    public ObtenerArticuloDetalleUseCase(IConsultaArticulosRepositorio consulta)
    {
        _consulta = consulta;
    }

    /// <summary>
    /// Devuelve SIEMPRE las seis modalidades. Los precios son los estampados en la tabla,
    /// sin recalcular: un precio ausente viaja con su razón, nunca como cero.
    /// </summary>
    public async Task<Result<FichaArticuloDto>> EjecutarAsync(int id, CancellationToken ct = default)
    {
        var detalle = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (detalle is null)
            return Result<FichaArticuloDto>.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");

        var precios = Enum.GetValues<Modalidad>()
            .Select(modalidad => Ficha(modalidad, detalle.PreciosPorModalidad.SingleOrDefault(p => p.Modalidad == modalidad)))
            .ToArray();

        return Result<FichaArticuloDto>.Success(new FichaArticuloDto(
            detalle.Id, detalle.CodigoInterno, detalle.Nombre, detalle.Activo, detalle.CostoVigente,
            detalle.UltimoProveedorId, detalle.Codigos, detalle.Existencias, precios,
            detalle.UbicacionDeposito, detalle.MotivoBaja));
    }

    private static PrecioModalidadFichaDto Ficha(Modalidad modalidad, PrecioModalidadConsultaDto? fila)
    {
        if (fila is null)
            return new(modalidad, null, null, null, false, null, null, RazonAusenciaPrecio.SinConfiguracion);

        return new(fila.Modalidad, fila.Base, fila.Porcentaje, fila.Valor, fila.CalculoManual,
            fila.FechaCalculo, fila.UsuarioId, RazonDeAusencia(fila));
    }

    /// <summary>
    /// El orden importa: un precio manual lo fija una persona y una modalidad sin base no
    /// tiene fórmula, así que ninguna de las dos se arregla con un costo. Con base y sin
    /// manual, el precio solo falta porque falta el valor de la base: el costo, directo
    /// (Lista, Costo) o a través del precio de Lista (Efectivo). El costo lo escribe la
    /// compra y el recálculo es de la misma operación (Fases 5 y 6).
    /// </summary>
    private static RazonAusenciaPrecio? RazonDeAusencia(PrecioModalidadConsultaDto fila)
    {
        if (fila.Valor is not null) return null;
        if (fila.CalculoManual) return RazonAusenciaPrecio.CasoEspecialNoFijado;
        if (fila.Base is null) return RazonAusenciaPrecio.TasaSinDefinir;
        return RazonAusenciaPrecio.SinCosto;
    }
}

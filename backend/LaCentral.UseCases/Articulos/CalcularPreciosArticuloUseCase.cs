using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>
/// D1 · HU-ART-05: la única regla de cálculo de precios del sistema. La API y el frontend
/// no duplican fórmulas. La usa el recálculo al confirmar una factura de compra (CA5).
/// precio = valor_de_la_base × (1 + porcentaje / 100)
/// </summary>
public class CalcularPreciosArticuloUseCase
{
    // articulo_precio_modalidad.precio es NUMERIC(12,2): la parte entera tiene a lo sumo
    // 10 dígitos. Ya viene redondeado a centavos.
    private const decimal LimitePrecio = 10_000_000_000m;

    private readonly IPreciosArticuloRepositorio _precios;

    public CalcularPreciosArticuloUseCase(IPreciosArticuloRepositorio precios) => _precios = precios;

    /// <summary>
    /// Recalcula y persiste los precios de los artículos pedidos. NO abre una unidad de
    /// trabajo: corre dentro de la de quien lo llama (la compra), así que si algo falla
    /// después, el recálculo se descarta con todo lo demás. Un precio que la columna no
    /// guarda es Invalido y no se escribe nada: primero se calcula todo, después se escribe.
    /// </summary>
    public async Task<Result> EjecutarAsync(IReadOnlyCollection<int> articuloIds, DateTime ahora, CancellationToken ct = default)
    {
        var actuales = await _precios.ObtenerAsync(articuloIds, ct);
        var recalculados = actuales
            .Where(a => a.Value.Precios.Count > 0)
            .Select(a => (Id: a.Key, Precios: Recalcular(a.Value.Precios, a.Value.CostoVigente, ahora)))
            .ToList();

        foreach (var (id, precios) in recalculados)
            if (precios.FirstOrDefault(p => p.Precio >= LimitePrecio) is { } excedido)
                return Result.Failure(TipoError.Invalido,
                    $"El precio {excedido.Modalidad} del artículo {id} ({excedido.Precio:N2}) superaría el máximo que se puede registrar.");

        foreach (var (id, precios) in recalculados)
            await _precios.ActualizarPreciosAsync(id, precios, ct);
        return Result.Success();
    }

    /// <summary>
    /// Null cuando no hay fórmula (sin porcentaje: manual o tasa sin definir) o cuando falta
    /// el valor de la base. Nunca cero por defecto, y nunca se sustituye una base por otra.
    /// Una fila manual nunca tiene porcentaje (CHECK manual_sin_porcentaje), y Recalcular
    /// no le pide precio a las manuales.
    /// </summary>
    public static decimal? Calcular(PrecioModalidad configuracion, decimal? costoArticulo, decimal? precioLista)
    {
        if (configuracion.Porcentaje is not { } porcentaje)
            return null;

        var valorBase = configuracion.Base switch
        {
            BasePrecio.CostoArticulo => costoArticulo,
            BasePrecio.PrecioLista => precioLista,
            _ => null
        };
        if (valorBase is null)
            return null;

        return Math.Round(valorBase.Value * (1m + porcentaje / 100m), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Las filas de un artículo con el precio de cada modalidad automática recalculado a
    /// partir del costo, como filas nuevas. Las filas manuales salen intactas (la política
    /// vigente: el recálculo no pisa un precio fijado por una persona). Lista va primero
    /// porque es la base de Efectivo; si Lista es manual, la base es su precio (D11).
    /// </summary>
    public static IReadOnlyList<PrecioModalidad> Recalcular(
        IReadOnlyList<PrecioModalidad> configuracion, decimal? costoArticulo, DateTime ahora)
    {
        var lista = configuracion.SingleOrDefault(p => p.Modalidad == Modalidad.Lista);
        var precioLista = lista is null ? null
            : lista.CalculoManual ? lista.Precio
            : Calcular(lista, costoArticulo, precioLista: null);

        return configuracion.Select(fila =>
        {
            if (fila.CalculoManual)
                return fila;
            var precio = Calcular(fila, costoArticulo, precioLista);
            return fila with { Precio = precio, FechaCalculo = precio is null ? null : ahora };
        }).ToList();
    }
}

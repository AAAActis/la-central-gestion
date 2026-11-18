using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-04 (028) · Anulación de factura de compra, con doble certificación.</summary>
public class AnularFacturaCompraUseCase
{
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IServicioHash _hash;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;
    private readonly CalcularPreciosArticuloUseCase _calcularPrecios;

    public AnularFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IUsuarioRepositorio usuarios,
        IServicioHash hash,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto,
        CalcularPreciosArticuloUseCase calcularPrecios)
    {
        _facturas = facturas;
        _usuarios = usuarios;
        _hash = hash;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
        _calcularPrecios = calcularPrecios;
    }

    public async Task<Result> EjecutarAsync(int facturaId, AnularFacturaCompraRequest request, CancellationToken ct = default)
    {
        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Result.Failure(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");
        if (string.IsNullOrWhiteSpace(request.Motivo))
            return Result.Failure(TipoError.Invalido, "El motivo de la anulación es obligatorio.");

        var factura = await _facturas.ObtenerParaAnularAsync(facturaId, ct);
        if (factura is null)
            return Result.Failure(TipoError.NoEncontrado, "La factura de compra indicada no existe.");
        if (!factura.Activa)
            return Result.Failure(TipoError.Conflicto, $"La factura {factura.Numero} ya está anulada.");

        // Doble certificación (B.4 del 07): otra persona, ADMINISTRADOR y activa, con su contraseña.
        var autoriza = await DobleCertificacion.VerificarAsync(_usuarios, _hash, request.UsuarioAutoriza, request.ContrasenaAutoriza, usuarioId, ct);
        if (!autoriza.IsSuccess)
            return Result.Failure(autoriza.Tipo, autoriza.Error);

        var anulacion = new AnulacionFacturaCompra(facturaId, request.Motivo.Trim(), usuarioId, autoriza.Value, DateTime.UtcNow);
        return await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            var anulada = await _facturas.AnularConEfectosAsync(anulacion, token);
            if (!anulada.IsSuccess)
                return anulada;
            // CA-001: los precios por modalidad siguen al costo devuelto, en la misma unidad de trabajo.
            return await _calcularPrecios.EjecutarAsync(factura.ArticuloIds, anulacion.Fecha, token);
        }, ct);
    }
}

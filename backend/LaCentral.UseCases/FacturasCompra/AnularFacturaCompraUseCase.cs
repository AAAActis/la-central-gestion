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

    // Valores de la tabla rol: 1 = OPERADOR, 2 = ADMINISTRADOR (ver AuthenticarUsuarioCase).
    private const int RolAdministrador = 2;
    // Un solo mensaje para usuario inexistente, inactivo o clave mala: no revela quién existe.
    private const string AutorizacionInvalida = "La autorización no es válida: revisá el usuario y la contraseña de quien autoriza.";

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
        var autoriza = string.IsNullOrWhiteSpace(request.UsuarioAutoriza)
            ? null
            : await _usuarios.ObtenerPorNombreAsync(request.UsuarioAutoriza.Trim(), ct);
        if (autoriza is null || !autoriza.Activo || request.ContrasenaAutoriza is null
            || !_hash.VerificarClave(request.ContrasenaAutoriza, autoriza.HashContrasena))
            return Result.Failure(TipoError.Invalido, AutorizacionInvalida);
        if (autoriza.Id == usuarioId)
            return Result.Failure(TipoError.Invalido, "La anulación la tiene que autorizar otra persona, no quien la pide.");
        if (autoriza.RolId != RolAdministrador)
            return Result.Failure(TipoError.Invalido, "Quien autoriza la anulación tiene que ser ADMINISTRADOR.");

        var anulacion = new AnulacionFacturaCompra(facturaId, request.Motivo.Trim(), usuarioId, autoriza.Id, DateTime.UtcNow);
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

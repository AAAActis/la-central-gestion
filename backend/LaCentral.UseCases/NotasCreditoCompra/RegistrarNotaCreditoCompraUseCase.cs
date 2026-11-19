using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.NotasCreditoCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.NotasCreditoCompra;

/// <summary>
/// HU-FCO-07 (057) · Registro de nota de crédito de proveedor. No toca costo, precios ni historial
/// de compra (regla a de Sergio, 18/11/2026): por eso no recibe el cálculo de precios.
/// </summary>
public class RegistrarNotaCreditoCompraUseCase
{
    // nota_credito_compra.numero es VARCHAR(30).
    private const int LargoMaximoNumero = 30;
    private const decimal TasaIva = 0.21m;

    private readonly INotaCreditoCompraRepositorio _notas;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IArticuloRepositorio _articulos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public RegistrarNotaCreditoCompraUseCase(INotaCreditoCompraRepositorio notas, IProveedorRepositorio proveedores,
        IFacturaCompraRepositorio facturas, IArticuloRepositorio articulos, IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
        => (_notas, _proveedores, _facturas, _articulos, _unidadDeTrabajo, _contexto) =
            (notas, proveedores, facturas, articulos, unidadDeTrabajo, contexto);

    public async Task<Result<RegistrarNotaCreditoCompraResponse>> EjecutarAsync(
        RegistrarNotaCreditoCompraRequest request, CancellationToken ct = default)
    {
        var validacion = ValidarDatos(request);
        if (!validacion.IsSuccess)
            return Fallo(validacion.Tipo, validacion.Error);
        var numero = request.Numero.Trim();
        var lineas = request.Lineas ?? [];

        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Fallo(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        var proveedor = await _proveedores.ObtenerDetallePorIdAsync(request.ProveedorId, ct);
        if (proveedor is null)
            return Fallo(TipoError.Invalido, $"El proveedor {request.ProveedorId} no existe.");
        if (!proveedor.Activo)
            return Fallo(TipoError.Invalido, $"El proveedor {proveedor.RazonSocial} está inactivo: no se le pueden registrar notas de crédito.");

        // CA-001: cada factura existe, es de este proveedor y está activa.
        var facturas = (await _notas.ObtenerFacturasAsync(request.FacturaIds!, ct)).ToDictionary(f => f.Id);
        foreach (var id in request.FacturaIds!)
        {
            if (!facturas.TryGetValue(id, out var factura))
                return Fallo(TipoError.Invalido, $"La factura de compra {id} no existe.");
            if (factura.ProveedorId != request.ProveedorId)
                return Fallo(TipoError.Invalido, $"La factura de compra {id} es de otro proveedor: la nota de crédito solo se asocia a facturas de {proveedor.RazonSocial}.");
            if (!factura.Activa)
                return Fallo(TipoError.Invalido, $"La factura de compra {id} está anulada.");
        }

        // CA-005: se devuelve lo que se compró, aunque el artículo se haya dado de baja después.
        if (lineas.Count > 0)
        {
            var articulos = await _facturas.ObtenerEstadoArticulosAsync(lineas.Select(l => l.ArticuloId).ToArray(), ct);
            if (lineas.FirstOrDefault(l => !articulos.ContainsKey(l.ArticuloId)) is { } sinArticulo)
                return Fallo(TipoError.Invalido, $"El artículo {sinArticulo.ArticuloId} no existe.");
            var origenes = await _articulos.ObtenerSucursalesAsync(ct);
            if (lineas.FirstOrDefault(l => !origenes.Contains(l.SucursalId)) is { } sinOrigen)
                return Fallo(TipoError.Invalido, $"El origen {sinOrigen.SucursalId} de la línea del artículo {sinOrigen.ArticuloId} no existe.");
        }

        // CA-003: validación previa; ante dos registros simultáneos decide ux_nota_credito_compra_numero_activa (solo entre vigentes).
        if (await _notas.ExisteNumeroAsync(request.ProveedorId, numero, ct))
            return Fallo(TipoError.Conflicto, $"El proveedor {proveedor.RazonSocial} ya tiene la nota de crédito {numero}.");

        var nota = new NotaCreditoCompra
        {
            ProveedorId = request.ProveedorId,
            Numero = numero,
            FechaEmision = request.FechaEmision,
            Clase = ClaseNormalizada(request.Clase)!,
            ImporteNeto = request.ImporteNeto,
            PercepcionIva = request.PercepcionIva,
            PercepcionIibb = request.PercepcionIibb,
            PercepcionMunicipal = request.PercepcionMunicipal,
            PercepcionGanancias = request.PercepcionGanancias,
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.UtcNow,
            DesdePdf = request.DesdePdf,
            FacturaIds = request.FacturaIds!.ToList(),
            Lineas = lineas.Select(l => new LineaNotaCreditoCompra
            {
                ArticuloId = l.ArticuloId, SucursalId = l.SucursalId, Cantidad = l.Cantidad
            }).ToList()
        };

        RegistrarNotaCreditoCompraResponse? registrada = null;
        var resultado = await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            // CA-014 (D-S6-25): dentro de la transacción, con las facturas bloqueadas por el repositorio.
            if (nota.Clase == NotaCreditoCompra.Devolucion)
            {
                var cantidades = await ValidarCantidadesAsync(nota, token);
                if (!cantidades.IsSuccess)
                    return cantidades;
            }
            var escrita = await _notas.RegistrarConEfectosAsync(nota, token);
            if (escrita.IsSuccess)
                registrada = new RegistrarNotaCreditoCompraResponse(escrita.Value);
            return escrita;
        }, ct);

        return resultado.IsSuccess
            ? Result<RegistrarNotaCreditoCompraResponse>.Success(registrada!)
            : Fallo(resultado.Tipo, resultado.Error);
    }

    private async Task<Result> ValidarCantidadesAsync(NotaCreditoCompra nota, CancellationToken ct)
    {
        var compradas = (await _notas.ObtenerCompradoYDevueltoAsync(nota.FacturaIds, ct)).ToDictionary(c => c.ArticuloId);
        foreach (var articulo in nota.Lineas.GroupBy(l => l.ArticuloId))
        {
            if (!compradas.TryGetValue(articulo.Key, out var c))
                return Result.Failure(TipoError.Invalido, $"El artículo {articulo.Key} no figura en las facturas de la nota de crédito: solo se devuelve lo que se compró en ellas.");
            var queda = c.Comprado - c.Devuelto;
            if (articulo.Sum(l => l.Cantidad) > queda)
                return Result.Failure(TipoError.Invalido,
                    $"Del artículo {articulo.Key} se compraron {c.Comprado:0.###} en esas facturas y ya se devolvieron {c.Devuelto:0.###}: se pueden devolver hasta {queda:0.###}.");
        }
        return Result.Success();
    }

    /// <summary>CA-007: los totales de la nota, sin guardar nada. Es siempre A: 21 % de IVA.</summary>
    public Result<TotalesNotaCreditoCompra> CalcularTotales(RegistrarNotaCreditoCompraRequest request)
    {
        var validacion = ValidarDatos(request);
        if (!validacion.IsSuccess)
            return Result<TotalesNotaCreditoCompra>.Failure(validacion.Tipo, validacion.Error);

        var neto = request.ImporteNeto;
        var iva = Math.Round(neto * TasaIva, 2, MidpointRounding.AwayFromZero);
        var percepciones = (request.PercepcionIva ?? 0) + (request.PercepcionIibb ?? 0) +
                           (request.PercepcionMunicipal ?? 0) + (request.PercepcionGanancias ?? 0);
        return Result<TotalesNotaCreditoCompra>.Success(new(neto, iva, percepciones, neto + iva + percepciones));
    }

    private static string? ClaseNormalizada(string? clase) => clase?.Trim().ToUpperInvariant() switch
    {
        NotaCreditoCompra.Bonificacion => NotaCreditoCompra.Bonificacion,
        NotaCreditoCompra.Devolucion => NotaCreditoCompra.Devolucion,
        _ => null
    };

    private static Result ValidarDatos(RegistrarNotaCreditoCompraRequest request)
    {
        // CA-003: siempre A, el número se copia exacto: ni tipo ni 99 que validar.
        if (string.IsNullOrWhiteSpace(request.Numero))
            return Result.Failure(TipoError.Invalido, "El número de la nota de crédito es obligatorio.");
        if (request.Numero.Trim().Length > LargoMaximoNumero)
            return Result.Failure(TipoError.Invalido, $"El número de la nota de crédito no puede superar los {LargoMaximoNumero} caracteres.");
        var clase = ClaseNormalizada(request.Clase);
        if (clase is null)
            return Result.Failure(TipoError.Invalido, "La clase de la nota de crédito es obligatoria: BONIFICACION o DEVOLUCION.");
        if (request.ImporteNeto <= 0 || !EntraEnColumna(request.ImporteNeto, Importe))
            return Result.Failure(TipoError.Invalido, "El importe neto tiene que ser mayor que cero, con hasta 2 decimales y 12 dígitos enteros.");
        var percepciones = new[] { request.PercepcionIva, request.PercepcionIibb, request.PercepcionMunicipal, request.PercepcionGanancias };
        if (percepciones.Any(p => p < 0 || (p is { } v && !EntraEnColumna(v, Importe))))
            return Result.Failure(TipoError.Invalido, "Las percepciones no pueden ser negativas y admiten hasta 2 decimales y 12 dígitos enteros.");

        // CA-001 y CA-002: al menos una factura, cada una una sola vez (clave primaria de la relación).
        if (request.FacturaIds is null || request.FacturaIds.Count == 0)
            return Result.Failure(TipoError.Invalido, "La nota de crédito tiene que referenciar al menos una factura de compra del proveedor.");
        if (request.FacturaIds.Distinct().Count() != request.FacturaIds.Count)
            return Result.Failure(TipoError.Invalido, "Una factura de compra figura más de una vez en la nota de crédito.");

        // CA-004 y CA-005: la bonificación no mueve stock; la devolución sí, línea por línea.
        var lineas = request.Lineas ?? [];
        if (clase == NotaCreditoCompra.Bonificacion && lineas.Count > 0)
            return Result.Failure(TipoError.Invalido, "Una nota de crédito por bonificación no lleva artículos: no mueve stock.");
        if (clase == NotaCreditoCompra.Devolucion && lineas.Count == 0)
            return Result.Failure(TipoError.Invalido, "Una nota de crédito por devolución tiene que tener al menos un artículo devuelto.");
        if (lineas.FirstOrDefault(l => l.Cantidad <= 0 || !EntraEnColumna(l.Cantidad, Cantidad)) is { } malCantidad)
            return Result.Failure(TipoError.Invalido, $"La cantidad del artículo {malCantidad.ArticuloId} tiene que ser mayor que cero, con hasta 3 decimales y 9 dígitos enteros.");
        if (lineas.GroupBy(l => (l.ArticuloId, l.SucursalId)).FirstOrDefault(g => g.Count() > 1) is { } repetido)
            return Result.Failure(TipoError.Invalido,
                $"El artículo {repetido.Key.ArticuloId} aparece en más de una línea con origen en la sucursal {repetido.Key.SucursalId}.");

        return Result.Success();
    }

    private static readonly (int Precision, int Escala) Cantidad = (12, 3);
    private static readonly (int Precision, int Escala) Importe = (14, 2);

    private static bool EntraEnColumna(decimal valor, (int Precision, int Escala) columna)
    {
        var limite = 1m;
        for (var i = 0; i < columna.Precision - columna.Escala; i++) limite *= 10;
        return valor == Math.Round(valor, columna.Escala) && Math.Abs(valor) < limite;
    }

    private static Result<RegistrarNotaCreditoCompraResponse> Fallo(TipoError tipo, string error) =>
        Result<RegistrarNotaCreditoCompraResponse>.Failure(tipo, error);
}

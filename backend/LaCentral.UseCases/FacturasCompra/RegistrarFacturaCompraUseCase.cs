using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.FacturasCompra.Dtos;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.FacturasCompra;

/// <summary>HU-FCO-01 · Registro manual de factura de compra.</summary>
public class RegistrarFacturaCompraUseCase
{
    // factura_compra.numero es VARCHAR(30).
    private const int LargoMaximoNumero = 30;

    private readonly IFacturaCompraRepositorio _facturas;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IArticuloRepositorio _articulos;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public RegistrarFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IProveedorRepositorio proveedores,
        IArticuloRepositorio articulos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
    {
        _facturas = facturas;
        _proveedores = proveedores;
        _articulos = articulos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    public async Task<Result<RegistrarFacturaCompraResponse>> EjecutarAsync(
        RegistrarFacturaCompraRequest request, CancellationToken ct = default)
    {
        var validacion = ValidarDatos(request);
        if (!validacion.IsSuccess)
            return Fallo(validacion.Tipo, validacion.Error);
        var numero = request.Numero.Trim();
        var lineas = request.Lineas!;

        var usuarioId = _contexto.UsuarioId;
        if (usuarioId <= 0)
            return Fallo(TipoError.NoAutorizado, "No se pudo determinar el usuario que ejecuta la operación.");

        // CA1: proveedor activo.
        var proveedor = await _proveedores.ObtenerDetallePorIdAsync(request.ProveedorId, ct);
        if (proveedor is null)
            return Fallo(TipoError.Invalido, $"El proveedor {request.ProveedorId} no existe.");
        if (!proveedor.Activo)
            return Fallo(TipoError.Invalido, $"El proveedor {proveedor.RazonSocial} está inactivo: no se le pueden registrar compras.");

        var articulos = await _facturas.ObtenerEstadoArticulosAsync(lineas.Select(l => l.ArticuloId).ToArray(), ct);
        foreach (var linea in lineas)
        {
            if (!articulos.TryGetValue(linea.ArticuloId, out var activo))
                return Fallo(TipoError.Invalido, $"El artículo {linea.ArticuloId} no existe.");
            if (!activo)
                return Fallo(TipoError.Invalido, $"El artículo {linea.ArticuloId} está dado de baja: reactivalo antes de registrarle una compra.");
        }

        // D9: el destino de cada línea tiene que ser una sucursal real.
        var sucursales = await _articulos.ObtenerSucursalesAsync(ct);
        if (lineas.FirstOrDefault(l => !sucursales.Contains(l.SucursalId)) is { } sinDestino)
            return Fallo(TipoError.Invalido, $"La sucursal {sinDestino.SucursalId} de la línea del artículo {sinDestino.ArticuloId} no existe.");

        // CA2: validación previa. Ante dos registros simultáneos decide el índice
        // ux_factura_compra_numero_activa, que el manejador global traduce a 409.
        var existente = await _facturas.ObtenerNumeroActivoAsync(request.ProveedorId, numero, ct);
        if (existente is not null)
            return Fallo(TipoError.Conflicto,
                $"El proveedor {proveedor.RazonSocial} ya tiene la factura {existente.Numero} activa " +
                $"(Id {existente.Id}, emitida el {existente.FechaEmision:dd/MM/yyyy}).");

        var factura = new FacturaCompra
        {
            ProveedorId = request.ProveedorId,
            Numero = numero,
            FechaEmision = request.FechaEmision,
            FechaVencimiento = request.FechaVencimiento,
            PercepcionIva = request.PercepcionIva,
            PercepcionIibb = request.PercepcionIibb,
            PercepcionMunicipal = request.PercepcionMunicipal,
            PercepcionGanancias = request.PercepcionGanancias,
            UsuarioId = usuarioId,
            FechaRegistro = DateTime.UtcNow,
            Lineas = lineas.Select(l => new FacturaCompraDetalle
            {
                ArticuloId = l.ArticuloId,
                SucursalId = l.SucursalId,
                Cantidad = l.Cantidad,
                PrecioUnitario = l.PrecioUnitario,
                DescuentoPorcentaje = l.DescuentoPorcentaje
            }).ToList()
        };

        RegistrarFacturaCompraResponse? registrada = null;
        var resultado = await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            registrada = new RegistrarFacturaCompraResponse(await _facturas.RegistrarConEfectosAsync(factura, token));
            // CA5 (Fase 6, HU-ART-05): acá, en la MISMA unidad de trabajo, se recalculan los
            // precios por modalidad de los artículos comprados. #32 no se cierra sin eso.
            return Result.Success();
        }, ct);

        // Si la unidad de trabajo dio éxito, la operación terminó y asignó `registrada`.
        return resultado.IsSuccess
            ? Result<RegistrarFacturaCompraResponse>.Success(registrada!)
            : Fallo(resultado.Tipo, resultado.Error);
    }

    private static Result ValidarDatos(RegistrarFacturaCompraRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Numero))
            return Result.Failure(TipoError.Invalido, "El número de factura es obligatorio.");
        if (request.Numero.Trim().Length > LargoMaximoNumero)
            return Result.Failure(TipoError.Invalido, $"El número de factura no puede superar los {LargoMaximoNumero} caracteres.");
        if (request.FechaVencimiento < request.FechaEmision)
            return Result.Failure(TipoError.Invalido, "El vencimiento no puede ser anterior a la emisión.");
        // CA4/D8: las percepciones se guardan como datos; solo se exige que no sean negativas.
        if (request.PercepcionIva < 0 || request.PercepcionIibb < 0 ||
            request.PercepcionMunicipal < 0 || request.PercepcionGanancias < 0)
            return Result.Failure(TipoError.Invalido, "Las percepciones no pueden ser negativas.");

        // CA3: sin líneas no hay compra.
        if (request.Lineas is null || request.Lineas.Count == 0)
            return Result.Failure(TipoError.Invalido, "La factura tiene que tener al menos una línea de detalle.");
        foreach (var linea in request.Lineas)
        {
            if (linea.Cantidad <= 0)
                return Result.Failure(TipoError.Invalido, $"La cantidad del artículo {linea.ArticuloId} tiene que ser mayor que cero.");
            if (linea.PrecioUnitario < 0)
                return Result.Failure(TipoError.Invalido, $"El precio unitario del artículo {linea.ArticuloId} no puede ser negativo.");
            if (linea.DescuentoPorcentaje < 0 || linea.DescuentoPorcentaje > 100)
                return Result.Failure(TipoError.Invalido, $"El descuento del artículo {linea.ArticuloId} tiene que estar entre 0 y 100.");
        }
        // Una línea por artículo: es la clave primaria del detalle.
        if (request.Lineas.GroupBy(l => l.ArticuloId).FirstOrDefault(g => g.Count() > 1) is { } repetido)
            return Result.Failure(TipoError.Invalido, $"El artículo {repetido.Key} aparece en más de una línea.");

        return Result.Success();
    }

    private static Result<RegistrarFacturaCompraResponse> Fallo(TipoError tipo, string error) =>
        Result<RegistrarFacturaCompraResponse>.Failure(tipo, error);
}

using LaCentral.UseCases.Articulos;
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
    private readonly CalcularPreciosArticuloUseCase _calcularPrecios;

    public RegistrarFacturaCompraUseCase(
        IFacturaCompraRepositorio facturas,
        IProveedorRepositorio proveedores,
        IArticuloRepositorio articulos,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto,
        CalcularPreciosArticuloUseCase calcularPrecios)
    {
        _facturas = facturas;
        _proveedores = proveedores;
        _articulos = articulos;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
        _calcularPrecios = calcularPrecios;
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
            var escrita = await _facturas.RegistrarConEfectosAsync(factura, token);
            if (!escrita.IsSuccess)
                return escrita;
            registrada = new RegistrarFacturaCompraResponse(escrita.Value);
            // CA5 (HU-ART-05 CA-001): en la MISMA unidad de trabajo, los precios por modalidad
            // de los comprados se recalculan con el costo que se acaba de escribir. Si el
            // recálculo rechaza, la unidad de trabajo descarta también la compra.
            return await _calcularPrecios.EjecutarAsync(
                factura.Lineas.Select(l => l.ArticuloId).ToArray(), factura.FechaRegistro, token);
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
        // CA4/D8: las percepciones se guardan como datos; solo se exige que no sean negativas
        // y que entren en su columna.
        if (request.PercepcionIva < 0 || request.PercepcionIibb < 0 ||
            request.PercepcionMunicipal < 0 || request.PercepcionGanancias < 0)
            return Result.Failure(TipoError.Invalido, "Las percepciones no pueden ser negativas.");
        if (!new[] { request.PercepcionIva, request.PercepcionIibb, request.PercepcionMunicipal, request.PercepcionGanancias }
                .All(p => EntraEnColumna(p, Importe)))
            return Result.Failure(TipoError.Invalido, "Las percepciones admiten hasta 2 decimales y 12 dígitos enteros.");

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
            if (!EntraEnColumna(linea.Cantidad, Cantidad))
                return Result.Failure(TipoError.Invalido, $"La cantidad del artículo {linea.ArticuloId} admite hasta 3 decimales y 9 dígitos enteros.");
            if (!EntraEnColumna(linea.PrecioUnitario, Importe))
                return Result.Failure(TipoError.Invalido, $"El precio unitario del artículo {linea.ArticuloId} admite hasta 2 decimales y 12 dígitos enteros.");
            if (!EntraEnColumna(linea.DescuentoPorcentaje, Descuento))
                return Result.Failure(TipoError.Invalido, $"El descuento del artículo {linea.ArticuloId} admite hasta 2 decimales.");
        }
        // Una línea por artículo: es la clave primaria del detalle.
        if (request.Lineas.GroupBy(l => l.ArticuloId).FirstOrDefault(g => g.Count() > 1) is { } repetido)
            return Result.Failure(TipoError.Invalido, $"El artículo {repetido.Key} aparece en más de una línea.");

        return Result.Success();
    }

    // NUMERIC(precisión, escala) de las columnas: lo que la base no puede guardar tal cual
    // (la redondearía, la rechazaría por un CHECK o por desborde) se rechaza acá como
    // Invalido, en lugar de terminar en un 500.
    private static readonly (int Precision, int Escala) Cantidad = (12, 3);   // detalle y stock
    private static readonly (int Precision, int Escala) Importe = (14, 2);    // precio, percepciones y costo
    private static readonly (int Precision, int Escala) Descuento = (6, 2);

    private static bool EntraEnColumna(decimal valor, (int Precision, int Escala) columna)
    {
        var limite = 1m;
        for (var i = 0; i < columna.Precision - columna.Escala; i++) limite *= 10;
        return valor == Math.Round(valor, columna.Escala) && Math.Abs(valor) < limite;
    }

    private static Result<RegistrarFacturaCompraResponse> Fallo(TipoError tipo, string error) =>
        Result<RegistrarFacturaCompraResponse>.Failure(tipo, error);
}

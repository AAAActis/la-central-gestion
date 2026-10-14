using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-01 · Alta de artículo.</summary>
public class CrearArticuloUseCase
{
    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;

    public CrearArticuloUseCase(
        IArticuloRepositorio articulos,
        IConsultaArticulosRepositorio consulta,
        IProveedorRepositorio proveedores,
        IUnidadDeTrabajo unidadDeTrabajo)
    {
        _articulos = articulos;
        _consulta = consulta;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
    }

    /// <summary>
    /// El código interno sale del Id que genera la base (identidad), no de MAX+1: dos altas
    /// simultáneas desde las dos sucursales no pueden chocar. La validación previa de códigos
    /// no cierra la carrera; la cierran los índices codigo_unico_global y un_codigo_por_proveedor,
    /// que el manejador global traduce a 409.
    /// </summary>
    public static string GenerarCodigoInterno(int id) => $"LC-{id:D6}";

    public async Task<Result<CrearArticuloResponse>> EjecutarAsync(CrearArticuloRequest request, CancellationToken ct = default)
    {
        var codigos = request.CodigosProveedor ?? Array.Empty<CodigoProveedorRequest>();
        var validacion = ArticuloValidaciones.ValidarDatos(request.Nombre, request.UbicacionDeposito);
        if (validacion.IsSuccess)
            validacion = ArticuloValidaciones.ValidarCodigos(codigos);
        if (!validacion.IsSuccess)
            return Result<CrearArticuloResponse>.Failure(validacion.Tipo, validacion.Error);

        foreach (var codigo in codigos.DistinctBy(c => c.ProveedorId))
        {
            var proveedor = await _proveedores.ObtenerDetallePorIdAsync(codigo.ProveedorId, ct);
            if (proveedor is null)
                return Result<CrearArticuloResponse>.Failure(TipoError.Invalido,
                    $"El proveedor {codigo.ProveedorId} no existe.");
            if (!proveedor.Activo)
                return Result<CrearArticuloResponse>.Failure(TipoError.Invalido,
                    $"El proveedor {proveedor.RazonSocial} está inactivo: no se le pueden asignar códigos.");
            // D2: un código vigente por proveedor y artículo.
            if (codigos.Count(c => c.ProveedorId == codigo.ProveedorId) > 1)
                return Result<CrearArticuloResponse>.Failure(TipoError.Conflicto,
                    $"El artículo solo puede tener un código del proveedor {proveedor.RazonSocial}.");
        }

        foreach (var codigo in codigos)
        {
            var clave = NormalizacionArticulo.Codigo(codigo.Codigo);
            var duenio = (await _consulta.ObtenerConflictosCodigoAsync(clave, ct)).FirstOrDefault();
            if (duenio is not null)
                return Result<CrearArticuloResponse>.Failure(TipoError.Conflicto,
                    $"El código {clave} ya pertenece al artículo {duenio.CodigoInterno} ({duenio.Nombre}).");
        }

        var articulo = new Articulo
        {
            Nombre = request.Nombre.Trim(),
            UbicacionDeposito = string.IsNullOrWhiteSpace(request.UbicacionDeposito) ? null : request.UbicacionDeposito.Trim(),
            Activo = true,
            CostoVigente = null,
            // La misma normalización con la que se busca.
            CodigosAlternativos = codigos
                .Select(c => new CodigoAlternativo(NormalizacionArticulo.Codigo(c.Codigo), c.ProveedorId)).ToList(),
            Existencias = (await _articulos.ObtenerSucursalesAsync(ct))
                .Select(s => new Existencia(s, 0m)).ToList(),
            Precios = ReglasPrecioModalidad.ConfiguracionInicial().ToList()
        };

        CrearArticuloResponse? creado = null;
        var resultado = await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            var id = await _articulos.AgregarAsync(articulo, token);
            var codigoInterno = GenerarCodigoInterno(id);
            if (!await _articulos.AsignarCodigoInternoAsync(id, codigoInterno, token))
                return Result.Failure(TipoError.NoEncontrado, "No se encontró el artículo recién creado para asignarle el código interno.");
            creado = new CrearArticuloResponse(id, codigoInterno);
            return Result.Success();
        }, ct);

        if (!resultado.IsSuccess)
            return Result<CrearArticuloResponse>.Failure(resultado.Tipo, resultado.Error);
        return creado is null
            ? Result<CrearArticuloResponse>.Failure(TipoError.Invalido, "El alta no se ejecutó.")
            : Result<CrearArticuloResponse>.Success(creado);
    }
}

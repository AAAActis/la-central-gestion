using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;

namespace LaCentral.UseCases.Articulos;

/// <summary>HU-ART-03 · Modificación de artículo y alta de códigos de proveedor.</summary>
public class ModificarArticuloUseCase
{
    private const string MensajeDadoDeBaja = "El artículo está dado de baja: reactivalo antes de modificarlo.";

    private readonly IArticuloRepositorio _articulos;
    private readonly IConsultaArticulosRepositorio _consulta;
    private readonly IProveedorRepositorio _proveedores;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo;
    private readonly IContextoUsuario _contexto;

    public ModificarArticuloUseCase(
        IArticuloRepositorio articulos,
        IConsultaArticulosRepositorio consulta,
        IProveedorRepositorio proveedores,
        IUnidadDeTrabajo unidadDeTrabajo,
        IContextoUsuario contexto)
    {
        _articulos = articulos;
        _consulta = consulta;
        _proveedores = proveedores;
        _unidadDeTrabajo = unidadDeTrabajo;
        _contexto = contexto;
    }

    /// <summary>
    /// CA1 actualiza por Id. CA2 suma los códigos nuevos sin tocar los existentes: un código
    /// que el artículo ya tiene no es conflicto, y uno omitido no se retira. CA3 rechaza el
    /// código de otro artículo informando cuál. CA5 deniega si está dado de baja. Cambiar el
    /// código interno es solo de ADMINISTRADOR, según el rol de la sesión.
    /// </summary>
    public async Task<Result> EjecutarAsync(int id, ModificarArticuloRequest request, CancellationToken ct = default)
    {
        var articulo = await _consulta.ObtenerDetallePorIdAsync(id, ct);
        if (articulo is null)
            return Result.Failure(TipoError.NoEncontrado, "El artículo indicado no existe.");
        if (!articulo.Activo)
            return Result.Failure(TipoError.Invalido, MensajeDadoDeBaja);

        var codigos = request.CodigosProveedor ?? Array.Empty<CodigoProveedorRequest>();
        var validacion = ArticuloValidaciones.ValidarDatos(request.Nombre, request.UbicacionDeposito);
        if (validacion.IsSuccess)
            validacion = ArticuloValidaciones.ValidarCodigos(codigos);
        if (!validacion.IsSuccess)
            return validacion;

        // null = no cambia: el UPDATE no toca la columna.
        string? codigoInterno = null;
        if (request.CodigoInterno is not null)
        {
            var clave = NormalizacionArticulo.Codigo(request.CodigoInterno);
            if (clave != NormalizacionArticulo.Codigo(articulo.CodigoInterno))
            {
                if (!_contexto.EsAdministrador)
                    return Result.Failure(TipoError.NoAutorizado, "Solo un administrador puede cambiar el código interno.");
                validacion = ArticuloValidaciones.ValidarCodigoInterno(clave);
                if (!validacion.IsSuccess)
                    return validacion;
                // Incluye al propio artículo: un código de proveedor suyo tampoco puede pasar a
                // ser su código interno.
                var duenio = (await _consulta.ObtenerConflictosCodigoAsync(clave, ct)).FirstOrDefault();
                if (duenio is not null)
                    return Result.Failure(TipoError.Conflicto,
                        $"El código {clave} ya pertenece al artículo {duenio.CodigoInterno} ({duenio.Nombre}).");
                codigoInterno = clave;
            }
        }

        // Los que el artículo ya tiene (mismo proveedor y misma clave) no se vuelven a agregar
        // ni se revalidan.
        var nuevos = codigos
            .Select(c => new CodigoAlternativo(NormalizacionArticulo.Codigo(c.Codigo), c.ProveedorId))
            .Where(c => !articulo.Codigos.Any(e => e.ProveedorId == c.ProveedorId && e.ClaveNormalizada == c.Codigo))
            .ToList();

        // El motor no relaciona codigo_interno con los códigos de proveedor: en la misma petición
        // tampoco pueden ser la misma clave.
        if (codigoInterno is not null && nuevos.Any(c => c.Codigo == codigoInterno))
            return Result.Failure(TipoError.Conflicto,
                $"El código {codigoInterno} no puede ser a la vez el código interno y un código de proveedor.");

        foreach (var codigo in nuevos.DistinctBy(c => c.ProveedorId))
        {
            var proveedor = await _proveedores.ObtenerDetallePorIdAsync(codigo.ProveedorId, ct);
            if (proveedor is null)
                return Result.Failure(TipoError.Invalido, $"El proveedor {codigo.ProveedorId} no existe.");
            if (!proveedor.Activo)
                return Result.Failure(TipoError.Invalido,
                    $"El proveedor {proveedor.RazonSocial} está inactivo: no se le pueden asignar códigos.");
            // D2: un código vigente por proveedor y artículo. Cambiarlo es retirar y agregar.
            if (articulo.Codigos.Any(e => e.ProveedorId == codigo.ProveedorId) ||
                nuevos.Count(c => c.ProveedorId == codigo.ProveedorId) > 1)
                return Result.Failure(TipoError.Conflicto,
                    $"El artículo solo puede tener un código del proveedor {proveedor.RazonSocial}.");
        }

        foreach (var codigo in nuevos)
        {
            var duenio = (await _consulta.ObtenerConflictosCodigoAsync(codigo.Codigo, ct)).FirstOrDefault();
            if (duenio is not null)
                return Result.Failure(TipoError.Conflicto,
                    $"El código {codigo.Codigo} ya pertenece al artículo {duenio.CodigoInterno} ({duenio.Nombre}).");
        }

        var nombre = request.Nombre.Trim();
        var ubicacion = string.IsNullOrWhiteSpace(request.UbicacionDeposito) ? null : request.UbicacionDeposito.Trim();

        return await _unidadDeTrabajo.EjecutarAsync(async token =>
        {
            // La condición "activo" va en la escritura: otra sesión pudo darlo de baja.
            if (!await _articulos.ActualizarDatosAsync(id, nombre, ubicacion, codigoInterno, token))
                return Result.Failure(TipoError.Invalido, MensajeDadoDeBaja);
            if (nuevos.Count > 0)
                await _articulos.AgregarCodigosAsync(id, nuevos, token);
            return Result.Success();
        }, ct);
    }
}

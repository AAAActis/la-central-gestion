using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Articulos;
using LaCentral.UseCases.Articulos.Dtos;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Articulos;

/// <summary>HU-ART-03 · lógica de la modificación con dobles. La persistencia se prueba contra PostgreSQL.</summary>
public class ModificarArticuloUseCaseTests
{
    private const int Id = 42;
    private const string CodigoActual = "LC-000042";

    private readonly Mock<IArticuloRepositorio> _articulos = new();
    private readonly Mock<IConsultaArticulosRepositorio> _consulta = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private Mock<IUnidadDeTrabajo> _unidad = DoblesBaja.UnidadQueEjecuta();
    private readonly Mock<IContextoUsuario> _contexto = DoblesBaja.Contexto();
    private IReadOnlyList<CodigoAlternativo>? _agregados;

    public ModificarArticuloUseCaseTests()
    {
        Articulo(activo: true, ("BPR-6ES", 10));
        _articulos.Setup(r => r.ActualizarDatosAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _articulos.Setup(r => r.AgregarCodigosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<CodigoAlternativo>>(),
                It.IsAny<CancellationToken>()))
            .Callback((int _, IReadOnlyList<CodigoAlternativo> c, CancellationToken _) => _agregados = c)
            .Returns(Task.CompletedTask);
        _consulta.Setup(c => c.ObtenerConflictosCodigoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ArticuloResumenConsultaDto>());
        Proveedor(10, activo: true, "Distribuidora Norte");
        Proveedor(20, activo: true, "Repuestos Sur");
        Operador();
    }

    private void Articulo(bool activo, params (string Codigo, int ProveedorId)[] codigos) =>
        _consulta.Setup(c => c.ObtenerDetallePorIdAsync(Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ArticuloDetalleConsultaDto(Id, CodigoActual, "Bujía", activo, null, null,
                codigos.Select(c => new CodigoProveedorConsultaDto(c.ProveedorId, $"Proveedor {c.ProveedorId}",
                    c.Codigo.ToLowerInvariant(), c.Codigo)).ToArray(),
                Array.Empty<StockSucursalConsultaDto>(), Array.Empty<PrecioModalidadConsultaDto>()));

    private void Proveedor(int id, bool activo, string razonSocial) =>
        _proveedores.Setup(p => p.ObtenerDetallePorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Proveedor
            {
                Id = id, RazonSocial = razonSocial, Activo = activo,
                MotivoBaja = activo ? null : "Cerró", FechaBaja = activo ? null : DateTime.UtcNow
            });

    private void Conflicto(string clave, int articuloId, string codigoInterno, string nombre) =>
        _consulta.Setup(c => c.ObtenerConflictosCodigoAsync(clave, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new ArticuloResumenConsultaDto(articuloId, codigoInterno, nombre, true) });

    private void Operador() => _contexto.SetupGet(c => c.EsAdministrador).Returns(false);
    private void Administrador() => _contexto.SetupGet(c => c.EsAdministrador).Returns(true);

    private ModificarArticuloUseCase Caso() =>
        new(_articulos.Object, _consulta.Object, _proveedores.Object, _unidad.Object, _contexto.Object);

    private static ModificarArticuloRequest Request(string? codigoInterno = null, params (int ProveedorId, string Codigo)[] codigos) =>
        new(" Bujía NGK ", " Estante 5 ", codigoInterno,
            codigos.Select(c => new CodigoProveedorRequest(c.ProveedorId, c.Codigo)).ToArray());

    private void NoSeEscribioNada()
    {
        _articulos.Verify(r => r.ActualizarDatosAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _articulos.Verify(r => r.AgregarCodigosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<CodigoAlternativo>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // codigoInterno null = el UPDATE no toca la columna (no se pisa un cambio de otra sesión).
    private void SeActualizo(string nombre, string? ubicacion, string? codigoInterno) =>
        _articulos.Verify(r => r.ActualizarDatosAsync(Id, nombre, ubicacion, It.Is<string>(c => c == codigoInterno), It.IsAny<CancellationToken>()),
            Times.Once);

    // ---- Existencia y CA5 ----

    [Fact]
    public async Task Inexistente_NoEncontrado()
    {
        var resultado = await Caso().EjecutarAsync(999, Request());

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        NoSeEscribioNada();
    }

    // CA5: dado de baja → se deniega e indica que debe reactivarse.
    [Fact]
    public async Task DadoDeBaja_InvalidoEIndicaQueHayQueReactivarlo()
    {
        Articulo(activo: false);

        var resultado = await Caso().EjecutarAsync(Id, Request());

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("reactiv", resultado.Error, StringComparison.OrdinalIgnoreCase);
        NoSeEscribioNada();
    }

    // Otra sesión lo dio de baja entre la lectura y la escritura: el UPDATE no encuentra
    // la fila activa, y no se agregan códigos.
    [Fact]
    public async Task DadoDeBajaAlEscribir_InvalidoYNoAgregaCodigos()
    {
        _articulos.Setup(r => r.ActualizarDatosAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "NGK-1")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("reactiv", resultado.Error, StringComparison.OrdinalIgnoreCase);
        _articulos.Verify(r => r.AgregarCodigosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<CodigoAlternativo>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Validaciones compartidas con el alta ----

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task NombreVacio_Invalido(string nombre)
    {
        var resultado = await Caso().EjecutarAsync(Id, Request() with { Nombre = nombre });

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task UbicacionDeMasDe60_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request() with { UbicacionDeposito = new string('u', 61) });

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task CodigoDeProveedorVacio_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "   ")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    // ---- CA1 ----

    [Fact]
    public async Task DatosValidos_ActualizaPorIdConLosDatosNormalizadosYConservaElCodigoInterno()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request());

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", "Estante 5", null);
        _articulos.Verify(r => r.AgregarCodigosAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<CodigoAlternativo>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UbicacionEnBlanco_SeGuardaComoNull()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request() with { UbicacionDeposito = "   " });

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", null, null);
    }

    [Fact]
    public async Task Escrituras_DentroDeLaUnidadDeTrabajo()
    {
        _unidad = DoblesBaja.UnidadQueNoEjecuta();

        await Caso().EjecutarAsync(Id, Request(null, (20, "NGK-1")));

        _unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        NoSeEscribioNada();
    }

    // ---- CA2 y CA3 ----

    // CA2: el código de otro proveedor se suma, normalizado; los existentes no se tocan.
    [Fact]
    public async Task CodigoDeOtroProveedor_SeAgregaSoloEseYConservaLosExistentes()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, " ngk-1 ")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_agregados);
        var agregado = Assert.Single(_agregados);
        Assert.Equal(("NGK-1", 20), (agregado.Codigo, agregado.ProveedorId));
        _articulos.Verify(r => r.RetirarCodigoAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // El mismo código del propio artículo NO es conflicto, aunque venga con otro formato.
    [Fact]
    public async Task ElMismoCodigoDelPropioArticulo_NoEsConflictoYNoSeVuelveAAgregar()
    {
        Conflicto("BPR-6ES", Id, CodigoActual, "Bujía");

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (10, " bpr-6es "), (20, "NGK-1")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        Assert.NotNull(_agregados);
        Assert.Equal(new[] { ("NGK-1", 20) }, _agregados.Select(c => (c.Codigo, c.ProveedorId)));
    }

    // Reenviar un código existente no revalida a su proveedor: dejó de estar activo y el
    // código igual sigue siendo del artículo.
    [Fact]
    public async Task CodigoExistenteDeUnProveedorInactivo_NoSeRevalida()
    {
        Proveedor(10, activo: false, "Distribuidora Norte");
        Conflicto("BPR-6ES", Id, CodigoActual, "Bujía");

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (10, "BPR-6ES")));

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", "Estante 5", null);
    }

    // CA3: el código ya pertenece a otro artículo → se rechaza informando cuál.
    [Fact]
    public async Task CodigoDeOtroArticulo_ConflictoInformandoCual()
    {
        Conflicto("NGK-1", 7, "LC-000007", "Filtro de aire");

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "ngk-1")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("LC-000007", resultado.Error);
        Assert.Contains("Filtro de aire", resultado.Error);
        NoSeEscribioNada();
    }

    // La unicidad es global: el propio artículo ya lo tiene, pero de OTRO proveedor.
    [Fact]
    public async Task CodigoQueElArticuloYaTieneDeOtroProveedor_Conflicto()
    {
        Conflicto("BPR-6ES", Id, CodigoActual, "Bujía");

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "BPR-6ES")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        NoSeEscribioNada();
    }

    // D2: un segundo código del mismo proveedor se rechaza. Cambiarlo es retirar + agregar.
    [Fact]
    public async Task SegundoCodigoDeUnProveedorQueYaTieneCodigo_Conflicto()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(null, (10, "BPR-7ES")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Distribuidora Norte", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task DosCodigosNuevosDelMismoProveedor_Conflicto()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "NGK-1"), (20, "NGK-2")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ProveedorInexistente_Invalido()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(null, (999, "X-1")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("999", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task ProveedorInactivo_Invalido()
    {
        Proveedor(20, activo: false, "Repuestos Sur");

        var resultado = await Caso().EjecutarAsync(Id, Request(null, (20, "NGK-1")));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("Repuestos Sur", resultado.Error);
        NoSeEscribioNada();
    }

    // ---- Código interno: solo ADMINISTRADOR ----

    [Fact]
    public async Task OperadorCambiaElCodigoInterno_NoAutorizadoYNoEscribe()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request("LC-ESPECIAL"));

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        NoSeEscribioNada();
    }

    // Mandar el mismo código interno (con otro formato) no es cambiarlo.
    [Fact]
    public async Task OperadorMandaElMismoCodigoInterno_NoEsUnCambio()
    {
        var resultado = await Caso().EjecutarAsync(Id, Request(" lc-000042 "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", "Estante 5", null);
    }

    [Fact]
    public async Task AdministradorCambiaElCodigoInterno_SeGuardaNormalizado()
    {
        Administrador();

        var resultado = await Caso().EjecutarAsync(Id, Request(" bujia-ngk-01 "));

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", "Estante 5", "BUJIA-NGK-01");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("12345678901234567890123456789012345678901")]
    public async Task CodigoInternoVacioOLargo_Invalido(string codigoInterno)
    {
        Administrador();

        var resultado = await Caso().EjecutarAsync(Id, Request(codigoInterno));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        NoSeEscribioNada();
    }

    // El borde aceptado: 40 caracteres entran en articulo.codigo_interno varchar(40).
    [Fact]
    public async Task CodigoInternoDeExactamente40_SeGuarda()
    {
        Administrador();
        var cuarenta = new string('B', 40);

        var resultado = await Caso().EjecutarAsync(Id, Request(cuarenta));

        Assert.True(resultado.IsSuccess, resultado.Error);
        SeActualizo("Bujía NGK", "Estante 5", cuarenta);
    }

    // LC-nnnnnn lo genera el sistema desde la identidad: uno puesto a mano chocaría con el
    // de un alta futura.
    [Fact]
    public async Task CodigoInternoConElPrefijoDelSistema_Invalido()
    {
        Administrador();

        var resultado = await Caso().EjecutarAsync(Id, Request("lc-999999"));

        Assert.Equal(TipoError.Invalido, resultado.Tipo);
        Assert.Contains("LC-", resultado.Error);
        NoSeEscribioNada();
    }

    [Fact]
    public async Task CodigoInternoDeOtroArticulo_ConflictoInformandoCual()
    {
        Administrador();
        Conflicto("BUJIA-01", 7, "BUJIA-01", "Filtro de aire");

        var resultado = await Caso().EjecutarAsync(Id, Request("bujia-01"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Filtro de aire", resultado.Error);
        NoSeEscribioNada();
    }

    // B1 del auditor: en la misma petición, el código interno nuevo y un código de proveedor
    // nuevo no pueden ser la misma clave. Ninguno está en la base todavía.
    [Fact]
    public async Task CodigoInternoIgualAUnCodigoNuevoDeLaMismaPeticion_Conflicto()
    {
        Administrador();

        var resultado = await Caso().EjecutarAsync(Id, Request("X-77", (20, " x-77 ")));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        NoSeEscribioNada();
    }

    // Un código de proveedor del propio artículo tampoco puede pasar a ser su código interno.
    [Fact]
    public async Task CodigoInternoIgualAUnCodigoDeProveedorDelPropioArticulo_Conflicto()
    {
        Administrador();
        Conflicto("BPR-6ES", Id, CodigoActual, "Bujía");

        var resultado = await Caso().EjecutarAsync(Id, Request("BPR-6ES"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        NoSeEscribioNada();
    }
}

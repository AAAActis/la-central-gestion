using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Entidades;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using LaCentral.UseCases.Puertos;
using Moq;

namespace LaCentral.Tests.Comun;

/// <summary>
/// D10 en los casos de uso: la baja abre el historial y la reactivación lo cierra,
/// con el usuario de la sesión y la fecha fijada por el caso de uso, dentro de la
/// unidad de trabajo. Que la fila se escriba lo prueban los tests de PostgreSQL.
/// </summary>
public class HistorialBajaCasosDeUsoTests
{
    private readonly Mock<IHistorialBajaRepositorio> _historial = new();
    private readonly Mock<IClienteRepositorio> _clientes = new();
    private readonly Mock<IProveedorRepositorio> _proveedores = new();
    private readonly Mock<IUsuarioRepositorio> _usuarios = new();

    private static Cliente ClienteActivo() => new() { Id = 10, Codigo = "CLI-010", Activo = true };
    private static Cliente ClienteInactivo() => new()
        { Id = 10, Codigo = "CLI-010", Activo = false, MotivoBaja = "Anterior", FechaBaja = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
    private static Proveedor ProveedorActivo() => new() { Id = 20, Codigo = "PRV-020", Activo = true };
    private static Proveedor ProveedorInactivo() => new() { Id = 20, Codigo = "PRV-020", Activo = false, MotivoBaja = "Anterior" };
    private static Usuario UsuarioActivo() => new() { Id = 30, NombreUsuario = "vendedor", RolId = 1, Activo = true };
    private static Usuario UsuarioInactivo() => new() { Id = 30, NombreUsuario = "vendedor", RolId = 1, Activo = false, MotivoBaja = "Licencia" };

    // ---------- Cliente ----------

    [Fact]
    public async Task DarDeBajaCliente_AbreHistorialConMotivoFechaYUsuarioDeLaSesion()
    {
        var cliente = ClienteActivo();
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        _clientes.Setup(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = new DarDeBajaClienteUseCase(_clientes.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);

        var resultado = await caso.EjecutarAsync(10, "CLI-010", "Cerró el negocio");

        Assert.True(resultado.IsSuccess);
        Assert.Equal(DoblesBaja.UsuarioEnSesion, cliente.UsuarioBajaId);
        _historial.Verify(h => h.AbrirAsync(EntidadConHistorialBaja.Cliente, 10, "Cerró el negocio",
            cliente.FechaBaja!.Value, DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DarDeBajaCliente_SinUsuarioEnSesion_DevuelveNoAutorizadoYNoEscribe()
    {
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteActivo());
        var caso = new DarDeBajaClienteUseCase(_clientes.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto(0).Object);

        var resultado = await caso.EjecutarAsync(10, "CLI-010", "Cerró el negocio");

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        _clientes.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DarDeBajaCliente_FilaInexistenteAlActualizar_DevuelveNoEncontradoYNoAbreHistorial()
    {
        var cliente = ClienteActivo();
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        _clientes.Setup(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var caso = new DarDeBajaClienteUseCase(_clientes.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);

        var resultado = await caso.EjecutarAsync(10, "CLI-010", "Cerró el negocio");

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DarDeBajaCliente_EscriturasOcurrenDentroDeLaUnidadDeTrabajo()
    {
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteActivo());
        var unidad = DoblesBaja.UnidadQueNoEjecuta();
        var caso = new DarDeBajaClienteUseCase(_clientes.Object, _historial.Object, unidad.Object, DoblesBaja.Contexto().Object);

        await caso.EjecutarAsync(10, "CLI-010", "Cerró el negocio");

        unidad.Verify(u => u.EjecutarAsync(It.IsAny<Func<CancellationToken, Task<Result>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _clientes.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReactivarCliente_CierraHistorialConFechaDelCasoDeUsoYUsuarioDeLaSesion()
    {
        var cliente = ClienteInactivo();
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(cliente);
        _clientes.Setup(r => r.ActualizarAsync(cliente, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = new ReactivarClienteUseCase(_clientes.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);
        var antes = DateTime.UtcNow;

        var resultado = await caso.EjecutarAsync(10);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("Anterior", cliente.MotivoBaja);
        _historial.Verify(h => h.CerrarAsync(EntidadConHistorialBaja.Cliente, 10,
            It.Is<DateTime>(f => f >= antes && f <= DateTime.UtcNow && f.Kind == DateTimeKind.Utc),
            DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivarCliente_SinUsuarioEnSesion_DevuelveNoAutorizadoYNoEscribe()
    {
        _clientes.Setup(r => r.ObtenerDetallePorIdAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ClienteInactivo());
        var caso = new ReactivarClienteUseCase(_clientes.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto(0).Object);

        var resultado = await caso.EjecutarAsync(10);

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        _clientes.Verify(r => r.ActualizarAsync(It.IsAny<Cliente>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    // ---------- Proveedor ----------

    [Fact]
    public async Task DarDeBajaProveedor_AbreHistorialConMotivoFechaYUsuarioDeLaSesion()
    {
        var proveedor = ProveedorActivo();
        _proveedores.Setup(r => r.ObtenerDetallePorIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(proveedor);
        var caso = new DarDeBajaProveedorUseCase(_proveedores.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);

        var resultado = await caso.EjecutarAsync(20, new DarDeBajaProveedorRequest("Dejó de operar", "PRV-020"));

        Assert.True(resultado.IsSuccess);
        _historial.Verify(h => h.AbrirAsync(EntidadConHistorialBaja.Proveedor, 20, "Dejó de operar",
            proveedor.FechaBaja!.Value, DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DarDeBajaProveedor_SinUsuarioEnSesion_DevuelveNoAutorizadoYNoEscribe()
    {
        _proveedores.Setup(r => r.ObtenerDetallePorIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(ProveedorActivo());
        var caso = new DarDeBajaProveedorUseCase(_proveedores.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto(0).Object);

        var resultado = await caso.EjecutarAsync(20, new DarDeBajaProveedorRequest("Dejó de operar", "PRV-020"));

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        _proveedores.Verify(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DarDeBajaProveedor_EscriturasOcurrenDentroDeLaUnidadDeTrabajo()
    {
        _proveedores.Setup(r => r.ObtenerDetallePorIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(ProveedorActivo());
        var caso = new DarDeBajaProveedorUseCase(_proveedores.Object, _historial.Object,
            DoblesBaja.UnidadQueNoEjecuta().Object, DoblesBaja.Contexto().Object);

        await caso.EjecutarAsync(20, new DarDeBajaProveedorRequest("Dejó de operar", "PRV-020"));

        _proveedores.Verify(r => r.ActualizarAsync(It.IsAny<Proveedor>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReactivarProveedor_CierraHistorialConFechaDelCasoDeUsoYUsuarioDeLaSesion()
    {
        _proveedores.Setup(r => r.ObtenerDetallePorIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(ProveedorInactivo());
        var caso = new ReactivarProveedorUseCase(_proveedores.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);
        var antes = DateTime.UtcNow;

        var resultado = await caso.EjecutarAsync(20);

        Assert.True(resultado.IsSuccess);
        _historial.Verify(h => h.CerrarAsync(EntidadConHistorialBaja.Proveedor, 20,
            It.Is<DateTime>(f => f >= antes && f <= DateTime.UtcNow && f.Kind == DateTimeKind.Utc),
            DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivarProveedor_SinUsuarioEnSesion_DevuelveNoAutorizadoYNoEscribe()
    {
        _proveedores.Setup(r => r.ObtenerDetallePorIdAsync(20, It.IsAny<CancellationToken>())).ReturnsAsync(ProveedorInactivo());
        var caso = new ReactivarProveedorUseCase(_proveedores.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto(0).Object);

        var resultado = await caso.EjecutarAsync(20);

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        _historial.VerifyNoOtherCalls();
    }

    // ---------- Usuario ----------

    [Fact]
    public async Task DarDeBajaUsuario_AbreHistorialConMotivoFechaYUsuarioDeLaSesion()
    {
        var usuario = UsuarioActivo();
        _usuarios.Setup(r => r.ObtenerPorIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarios.Setup(r => r.ActualizarAsync(usuario, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = new DarDeBajaUsuarioUseCase(_usuarios.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);

        var resultado = await caso.EjecutarAsync(30, "  Licencia  ");

        Assert.True(resultado.IsSuccess);
        _historial.Verify(h => h.AbrirAsync(EntidadConHistorialBaja.Usuario, 30, "Licencia",
            usuario.FechaBaja!.Value, DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DarDeBajaUsuario_EscriturasOcurrenDentroDeLaUnidadDeTrabajo()
    {
        _usuarios.Setup(r => r.ObtenerPorIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(UsuarioActivo());
        var caso = new DarDeBajaUsuarioUseCase(_usuarios.Object, _historial.Object,
            DoblesBaja.UnidadQueNoEjecuta().Object, DoblesBaja.Contexto().Object);

        await caso.EjecutarAsync(30, "Licencia");

        _usuarios.Verify(r => r.ActualizarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReactivarUsuario_CierraHistorialConFechaDelCasoDeUsoYUsuarioDeLaSesion()
    {
        var usuario = UsuarioInactivo();
        _usuarios.Setup(r => r.ObtenerPorIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(usuario);
        _usuarios.Setup(r => r.ActualizarAsync(usuario, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var caso = new ReactivarUsuarioUseCase(_usuarios.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto().Object);
        var antes = DateTime.UtcNow;

        var resultado = await caso.EjecutarAsync(30);

        Assert.True(resultado.IsSuccess);
        _historial.Verify(h => h.CerrarAsync(EntidadConHistorialBaja.Usuario, 30,
            It.Is<DateTime>(f => f >= antes && f <= DateTime.UtcNow && f.Kind == DateTimeKind.Utc),
            DoblesBaja.UsuarioEnSesion, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReactivarUsuario_SinUsuarioEnSesion_DevuelveNoAutorizadoYNoEscribe()
    {
        _usuarios.Setup(r => r.ObtenerPorIdAsync(30, It.IsAny<CancellationToken>())).ReturnsAsync(UsuarioInactivo());
        var caso = new ReactivarUsuarioUseCase(_usuarios.Object, _historial.Object,
            DoblesBaja.UnidadQueEjecuta().Object, DoblesBaja.Contexto(0).Object);

        var resultado = await caso.EjecutarAsync(30);

        Assert.Equal(TipoError.NoAutorizado, resultado.Tipo);
        _usuarios.Verify(r => r.ActualizarAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
        _historial.VerifyNoOtherCalls();
    }
}

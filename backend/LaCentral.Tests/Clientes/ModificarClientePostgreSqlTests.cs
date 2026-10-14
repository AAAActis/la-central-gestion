using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Clientes;
using LaCentral.UseCases.Clientes.Dtos;
using LaCentral.UseCases.Comun;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;
using ClienteBd = LaCentral.Data.Models.Cliente;
using ClienteDominio = LaCentral.UseCases.Entidades.Cliente;

namespace LaCentral.Tests.Clientes;

/// <summary>
/// CRÍTICO 3: la modificación se resuelve por Id, no por el código, que es mutable.
/// Toda afirmación relee la fila con un contexto nuevo.
/// </summary>
[Trait("Cliente", "PostgreSQL")]
public class ModificarClientePostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ModificarClientePostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    [PostgreSqlFact]
    public async Task CambiarCodigoAlDeOtroCliente_DaConflictoYElOtroQuedaIntacto()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (a, b) = await SembrarDosClientesAsync(fixture);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await new ModificarClienteUseCase(new ClienteRepositorio(contexto))
                .EjecutarAsync(a.Id, Pedido(codigo: b.Codigo, razonSocial: "A pisando a B"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains(b.Codigo, resultado.Error);
        await using var lectura = fixture.CrearContexto();
        var filaB = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == b.Id);
        var filaA = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == a.Id);
        Assert.Equal("Cliente B", filaB.RazonSocial);
        Assert.Equal("CLI-B", filaB.Codigo);
        Assert.Equal("Cliente A", filaA.RazonSocial);
        Assert.Equal("CLI-A", filaA.Codigo);
    }

    [PostgreSqlFact]
    public async Task CambiarCodigoAUnoLibre_PersisteEnLaMismaFila()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (a, b) = await SembrarDosClientesAsync(fixture);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await new ModificarClienteUseCase(new ClienteRepositorio(contexto))
                .EjecutarAsync(a.Id, Pedido(codigo: "CLI-A-NUEVO", razonSocial: "Cliente A renombrado"));

        Assert.True(resultado.IsSuccess, resultado.Error);
        await using var lectura = fixture.CrearContexto();
        var filaA = await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == a.Id);
        Assert.Equal("CLI-A-NUEVO", filaA.Codigo);
        Assert.Equal("Cliente A renombrado", filaA.RazonSocial);
        Assert.Equal(2, await lectura.Cliente.CountAsync());
        Assert.Equal("Cliente B", (await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == b.Id)).RazonSocial);
    }

    [PostgreSqlFact]
    public async Task ActualizarConIdInexistente_DevuelveFalseYNoTocaAlDuenioDelCodigo()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (a, _) = await SembrarDosClientesAsync(fixture);

        bool actualizado;
        await using (var contexto = fixture.CrearContexto())
            // Mismo código que A, Id que no existe: antes se resolvía por código y pisaba a A.
            actualizado = await new ClienteRepositorio(contexto).ActualizarAsync(new ClienteDominio
            {
                Id = a.Id + 1000, Codigo = a.Codigo, RazonSocial = "Fantasma",
                CondicionFiscal = "CONSUMIDOR_FINAL", CondicionPago = "Contado", Activo = true
            });

        Assert.False(actualizado);
        await using var lectura = fixture.CrearContexto();
        Assert.Equal("Cliente A", (await lectura.Cliente.AsNoTracking().SingleAsync(c => c.Id == a.Id)).RazonSocial);
    }

    // Caracterización: el comportamiento ya existía (ObtenerDetallePorIdAsync → null).
    // Verificado por mutación quitando el chequeo de null en ModificarClienteUseCase.
    [PostgreSqlFact]
    public async Task ModificarConIdInexistente_DevuelveNoEncontradoYNoTocaNada()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        var (a, b) = await SembrarDosClientesAsync(fixture);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await new ModificarClienteUseCase(new ClienteRepositorio(contexto))
                .EjecutarAsync(a.Id + b.Id + 1000, Pedido(codigo: a.Codigo, razonSocial: "Fantasma"));

        Assert.Equal(TipoError.NoEncontrado, resultado.Tipo);
        await using var lectura = fixture.CrearContexto();
        Assert.Equal(new[] { "Cliente A", "Cliente B" },
            await lectura.Cliente.AsNoTracking().OrderBy(c => c.Id).Select(c => c.RazonSocial).ToArrayAsync());
    }

    private static async Task<(ClienteBd A, ClienteBd B)> SembrarDosClientesAsync(PostgreSqlPruebas fixture)
    {
        var a = new ClienteBd { Codigo = "CLI-A", RazonSocial = "Cliente A", Activo = true };
        var b = new ClienteBd { Codigo = "CLI-B", RazonSocial = "Cliente B", Activo = true };
        await using var contexto = fixture.CrearContexto();
        contexto.Cliente.AddRange(a, b);
        await contexto.SaveChangesAsync();
        return (a, b);
    }

    private static ModificarClienteRequest Pedido(string codigo, string razonSocial) =>
        new(codigo, razonSocial, null, "CONSUMIDOR_FINAL", "Contado", new List<string>(), new List<string>());
}

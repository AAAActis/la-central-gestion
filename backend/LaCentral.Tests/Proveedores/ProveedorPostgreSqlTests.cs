using LaCentral.Data.Repositorios;
using LaCentral.Tests.Infraestructura;
using LaCentral.UseCases.Comun;
using LaCentral.UseCases.Proveedores;
using LaCentral.UseCases.Proveedores.Dtos;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace LaCentral.Tests.Proveedores;

/// <summary>Caso de uso + repositorio real; toda afirmación relee con un contexto nuevo.</summary>
[Trait("Proveedor", "PostgreSQL")]
public class ProveedorPostgreSqlTests
{
    private readonly ITestOutputHelper _salida;
    public ProveedorPostgreSqlTests(ITestOutputHelper salida) => _salida = salida;

    // ALTO 5
    [PostgreSqlFact]
    public async Task DosAltasConElMismoCodigo_LaSegundaDaConflictoIdentificandoAlPrimero()
    {
        await using var fixture = await PostgreSqlPruebas.CrearAsync(_salida);
        await using (var contexto = fixture.CrearContexto())
            Assert.True((await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-100", "Repuestos del Centro"))).IsSuccess);

        Result resultado;
        await using (var contexto = fixture.CrearContexto())
            resultado = await new CrearProveedorUseCase(new ProveedorRepositorio(contexto))
                .EjecutarAsync(Alta("PRV-100", "Otro Proveedor"));

        Assert.Equal(TipoError.Conflicto, resultado.Tipo);
        Assert.Contains("Repuestos del Centro", resultado.Error);
        await using var lectura = fixture.CrearContexto();
        var filas = await lectura.Proveedor.AsNoTracking().Where(p => p.Codigo == "PRV-100").ToListAsync();
        Assert.Equal("Repuestos del Centro", Assert.Single(filas).RazonSocial);
    }

    private static CrearProveedorRequest Alta(string codigo, string razonSocial) =>
        new(codigo, razonSocial, null, null, null, null);
}

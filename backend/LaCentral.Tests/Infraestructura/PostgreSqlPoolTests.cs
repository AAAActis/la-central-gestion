using Xunit.Abstractions;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// Con Maximum Pool Size = 1, los tests que abren dos conexiones a la vez se serializan
/// y pasan sin probar la concurrencia que declaran. El fixture tiene que rechazarlo
/// ANTES de conectarse: por eso la cadena apunta a un puerto cerrado, y si el fixture
/// llega a intentar la conexión el error es de red, no el esperado.
/// </summary>
[Collection(nameof(EntornoPostgreSqlSinParalelismo))]
public class PostgreSqlPoolTests
{
    private readonly ITestOutputHelper _salida;
    public PostgreSqlPoolTests(ITestOutputHelper salida) => _salida = salida;

    [Fact]
    public async Task PoolDeUnaConexion_FallaAntesDeConectarseExplicandoPorQue()
    {
        var variables = new[] { "LACENTRAL_TEST_PG", "LA_CENTRAL_TEST_POSTGRES" };
        var originales = variables.ToDictionary(v => v, Environment.GetEnvironmentVariable);
        try
        {
            foreach (var variable in variables)
                Environment.SetEnvironmentVariable(variable,
                    "Host=127.0.0.1;Port=1;Database=la_central_test;Username=nadie;Password=nada;Maximum Pool Size=1");

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => PostgreSqlPruebas.CrearAsync(_salida));

            Assert.Contains("Maximum Pool Size", error.Message);
            Assert.Contains("README", error.Message);
        }
        finally
        {
            foreach (var (variable, valor) in originales) Environment.SetEnvironmentVariable(variable, valor);
        }
    }
}

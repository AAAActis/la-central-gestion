using Xunit.Abstractions;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// Sin conexión de test, un [PostgreSqlFact] tiene que FALLAR. Si se omitiera, la
/// suite se ve verde sin haber tocado el motor real: el modo de falla que ya costó
/// una semana.
///
/// Estos tests vacían variables de entorno del proceso: corren en una colección
/// sin paralelismo para no dejar sin conexión a los tests que sí la usan.
/// </summary>
[Collection(nameof(EntornoPostgreSqlSinParalelismo))]
public class PostgreSqlObligatorioTests
{
    private readonly ITestOutputHelper _salida;
    public PostgreSqlObligatorioTests(ITestOutputHelper salida) => _salida = salida;

    [Fact]
    public void SinConexionConfigurada_UnPostgreSqlFactNoSeOmite()
    {
        using var _ = new EntornoSinConexion();

        Assert.Null(new PostgreSqlFactAttribute().Skip);
    }

    [Fact]
    public async Task SinConexionConfigurada_FallaExplicandoComoConfigurarla()
    {
        using var _ = new EntornoSinConexion();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => PostgreSqlPruebas.CrearAsync(_salida));

        Assert.Contains("LACENTRAL_TEST_PG", error.Message);
        Assert.Contains("README", error.Message);
    }

    private sealed class EntornoSinConexion : IDisposable
    {
        private static readonly string[] Variables =
            { "LA_CENTRAL_TEST_POSTGRES", "LACENTRAL_TEST_PG", "LA_CENTRAL_REQUIRE_POSTGRES" };
        private readonly Dictionary<string, string?> _originales =
            Variables.ToDictionary(v => v, Environment.GetEnvironmentVariable);

        public EntornoSinConexion()
        {
            foreach (var variable in Variables) Environment.SetEnvironmentVariable(variable, null);
        }

        public void Dispose()
        {
            foreach (var (variable, valor) in _originales) Environment.SetEnvironmentVariable(variable, valor);
        }
    }
}

[CollectionDefinition(nameof(EntornoPostgreSqlSinParalelismo), DisableParallelization = true)]
public class EntornoPostgreSqlSinParalelismo { }

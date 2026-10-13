namespace LaCentral.Tests.Infraestructura;

public class PostgreSqlSeguridadTests
{
    [Theory]
    [InlineData("la_central")]
    [InlineData("la_central_principal_test")]
    [InlineData("test_production")]
    [InlineData("prod_test")]
    [InlineData("test_prod")]
    [InlineData("")]
    public void Conexion_NoPermiteBasePrincipalOProduccion(string nombre)
        => Assert.Throws<InvalidOperationException>(() => PostgreSqlPruebas.ValidarBase(nombre));

    [Theory]
    [InlineData("la_central_test")]
    [InlineData("test_la_central")]
    public void Conexion_ExigeNombreDeBaseExclusivaDePruebas(string nombre)
        => PostgreSqlPruebas.ValidarBase(nombre);
}

using LaCentral.UseCases.Articulos;

namespace LaCentral.Tests.Articulos;

public class NormalizacionArticuloTests
{
    [Theory]
    [InlineData("  gn-001  ", "GN-001")]
    [InlineData("001", "001")]
    [InlineData("ñ-01", "Ñ-01")]
    public void Codigo_ConservaIdentidadYNormalizaEspaciosYMayusculas(string original, string esperado)
    {
        Assert.Equal(esperado, NormalizacionArticulo.Codigo(original));
        Assert.Equal(esperado, NormalizacionArticulo.Codigo(esperado));
    }

    [Fact]
    public void CodigosConGuionesOCerosDistintos_NoSeEquiparan()
    {
        Assert.NotEqual(NormalizacionArticulo.Codigo("GN-01"), NormalizacionArticulo.Codigo("GN01"));
        Assert.NotEqual(NormalizacionArticulo.Codigo("001"), NormalizacionArticulo.Codigo("1"));
    }

    [Fact]
    public void Alternativas_IgnoraFragmentosVaciosYDuplicados()
        => Assert.Equal(new[] { "MOUSE GENIUS 01", "GN-01", "GENIUS-01" },
            NormalizacionArticulo.Alternativas(" mouse genius 01 *gn-01*GN-01*genius-01* *"));

    [Fact]
    public void Nombre_NormalizaEspaciosYAcentosSinCambiarMarcaNiEnie()
    {
        Assert.Equal(new[] { "BUJIA", "PEQUEÑA", "GENIOS" },
            NormalizacionArticulo.Palabras(" bujía\tpequeña  genios "));
        Assert.NotEqual(NormalizacionArticulo.NombreParaBusqueda("GENIOS"),
            NormalizacionArticulo.NombreParaBusqueda("GENIUS"));
    }
}

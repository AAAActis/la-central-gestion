using LaCentral.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// Un test que corre sobre otra versión de EF Core que la de producción no prueba
/// producción. LaCentral.Data compila contra una versión de Relational; los tests
/// tienen que cargar exactamente esa.
/// </summary>
public class VersionesEfCoreTests
{
    [Fact]
    public void EfCoreRelational_LaVersionCargadaEsLaQueUsaLaCentralData()
    {
        var compilada = typeof(LaCentralDbContext).Assembly.GetReferencedAssemblies()
            .Single(a => a.Name == "Microsoft.EntityFrameworkCore.Relational").Version;
        var cargada = typeof(RelationalDatabaseFacadeExtensions).Assembly.GetName().Version;

        Assert.Equal(compilada, cargada);
    }
}

namespace LaCentral.Tests.Infraestructura;

/// <summary>
/// El bucle rápido es <c>dotnet test --filter "Base!=PostgreSQL"</c>. Una clase con
/// [PostgreSqlFact] sin el Trait se cuela en él y le pega a la base en cada iteración.
/// </summary>
public class TraitBasePostgreSqlTests
{
    [Fact]
    public void TodaClaseConPostgreSqlFact_LlevaElTraitBasePostgreSql()
    {
        var sinTrait = typeof(TraitBasePostgreSqlTests).Assembly.GetTypes()
            .Where(t => t.GetMethods().Any(m => m.IsDefined(typeof(PostgreSqlFactAttribute), false)))
            .Where(t => !t.GetCustomAttributesData().Any(a =>
                a.AttributeType == typeof(TraitAttribute)
                && (string?)a.ConstructorArguments[0].Value == "Base"
                && (string?)a.ConstructorArguments[1].Value == "PostgreSQL"))
            .Select(t => t.Name)
            .Order();

        Assert.Empty(sinTrait);
    }
}

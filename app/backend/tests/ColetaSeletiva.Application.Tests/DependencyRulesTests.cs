using System.Reflection;

namespace ColetaSeletiva.Application.Tests;

[TestClass]
public sealed class DependencyRulesTests
{
    [TestMethod]
    public void Application_nao_referencia_Infrastructure_nem_Api()
    {
        var referencias = ObterReferencias(typeof(ColetaSeletiva.Application.AssemblyReference).Assembly);

        Assert.IsFalse(
            referencias.Contains("ColetaSeletiva.Infrastructure"),
            "A camada Application não pode referenciar Infrastructure.");
        Assert.IsFalse(
            referencias.Contains("ColetaSeletiva.Api"),
            "A camada Application não pode referenciar Api.");
    }

    [TestMethod]
    public void Domain_nao_referencia_outras_camadas_nem_infraestrutura()
    {
        var referencias = ObterReferencias(typeof(ColetaSeletiva.Domain.AssemblyReference).Assembly);

        Assert.IsFalse(referencias.Contains("ColetaSeletiva.Application"));
        Assert.IsFalse(referencias.Contains("ColetaSeletiva.Infrastructure"));
        Assert.IsFalse(referencias.Contains("ColetaSeletiva.Api"));
        Assert.IsFalse(referencias.Any(nome => nome.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)));
        Assert.IsFalse(referencias.Any(nome => nome.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)));
    }

    private static HashSet<string> ObterReferencias(Assembly assembly)
    {
        return assembly
            .GetReferencedAssemblies()
            .Select(referencia => referencia.Name)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
    }
}

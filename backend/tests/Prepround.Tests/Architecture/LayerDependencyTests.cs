using System.Reflection;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Domain.Shared;
using Prepround.Infrastructure;

namespace Prepround.Tests.Architecture;

/// <summary>Clean Architecture direction (ADR 001): Domain ← Application ← Infrastructure ← Api.</summary>
public class LayerDependencyTests
{
    private static readonly string[] InfrastructurePrefixes =
        ["Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore", "YamlDotNet"];

    [Fact]
    public void Domain_references_only_the_base_class_library()
    {
        var references = ReferencedNames(typeof(Error).Assembly);

        Assert.All(references, name => Assert.True(
            name.StartsWith("System", StringComparison.Ordinal) || name is "netstandard" or "mscorlib",
            $"Domain references {name}."));
    }

    [Fact]
    public void Application_references_no_outer_layer_or_infrastructure_package()
    {
        var references = ReferencedNames(typeof(IQuery<>).Assembly);

        Assert.DoesNotContain("Prepround.Infrastructure", references);
        Assert.DoesNotContain("Prepround.Api", references);
        Assert.DoesNotContain(references, name => InfrastructurePrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)));
    }

    [Fact]
    public void Infrastructure_does_not_reference_the_api()
    {
        var references = ReferencedNames(typeof(PreproundContext).Assembly);

        Assert.DoesNotContain("Prepround.Api", references);
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    private static string[] ReferencedNames(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name!).ToArray();
}

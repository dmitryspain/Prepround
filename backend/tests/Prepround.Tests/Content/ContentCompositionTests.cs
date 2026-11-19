using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application;
using Prepround.Application.Content;
using Prepround.Infrastructure;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// The composition ContentTool builds (AddApplication + AddInfrastructure) and PRP-004 will resolve import from:
/// it must build strictly and give the read-and-check step without a connection string.
/// </summary>
public class ContentCompositionTests
{
    [Fact]
    public void Builds_strictly_without_a_connection_string_and_resolves_the_content_step_in_a_scope()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var reader = scope.ServiceProvider.GetRequiredService<IContentReader>();
        var loader = scope.ServiceProvider.GetRequiredService<ContentLoader>();

        Assert.IsType<YamlContentReader>(reader);
        Assert.NotNull(loader);
    }

    [Fact]
    public void Resolved_content_step_reads_and_checks_the_yaml_tree()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, "[alpha, beta]", "[alpha, beta, gamma]");

        var result = scope.ServiceProvider.GetRequiredService<ContentLoader>().Load(content.Root);

        var error = Assert.Single(result.Errors);
        Assert.Equal((ContentFixture.FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("gamma", error.Message);
    }

    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        return new ServiceCollection()
            .AddApplication()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}

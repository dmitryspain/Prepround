using Microsoft.Extensions.Configuration;
using Prepround.ContentTool;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// Precedence of ContentTool's configuration, checked over the very function Program uses, with an in-memory source
/// in place of the owner's user-secrets. Changes the process environment, so it runs alone (see the collection).
/// </summary>
[Collection(ProcessEnvironmentCollection.Name)]
public class ContentToolConfigurationTests
{
    private const string Variable = "ConnectionStrings__Prepround";
    private const string FromSecrets = "Host=secrets;Database=prepround";
    private const string FromEnvironment = "Host=environment;Database=prepround";

    [Theory]
    // An empty value cannot be set in-process (SetEnvironmentVariable deletes it); the real process test covers "".
    [InlineData(FromEnvironment, FromEnvironment)]
    [InlineData(null, FromSecrets)]
    public void Environment_variable_overrides_user_secrets(string? environmentValue, string expected)
    {
        var previous = Environment.GetEnvironmentVariable(Variable);
        Environment.SetEnvironmentVariable(Variable, environmentValue);
        try
        {
            var configuration = ContentToolConfiguration.Build(builder => builder.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Prepround"] = FromSecrets }));

            Assert.Equal(expected, configuration.GetConnectionString("Prepround"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Variable, previous);
        }
    }
}

/// <summary>Tests that change environment variables of the test process; child processes of other tests inherit them.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ProcessEnvironmentCollection
{
    public const string Name = "Process environment";
}

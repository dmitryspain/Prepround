using Microsoft.Extensions.Configuration;

namespace Prepround.ContentTool;

/// <summary>
/// Configuration of ContentTool: user-secrets first, environment variables on top, so
/// <c>ConnectionStrings__Prepround</c> overrides the secret (tests against Testcontainers and agents' commands rely on it).
/// A class rather than a local function in Program, so a test can check the precedence with a secrets source of its own
/// instead of the owner's real user-secrets.
/// </summary>
public static class ContentToolConfiguration
{
    /// <summary>What ContentTool runs with: the user-secrets shared with the API, then the environment.</summary>
    public static IConfigurationRoot Build() =>
        Build(builder => builder.AddUserSecrets(typeof(ContentToolConfiguration).Assembly, optional: true));

    /// <param name="addUserSecrets">Adds the user-secrets layer; a test passes an in-memory source in its place.</param>
    public static IConfigurationRoot Build(Action<IConfigurationBuilder> addUserSecrets)
    {
        var builder = new ConfigurationBuilder();
        addUserSecrets(builder);

        return builder
            .AddEnvironmentVariables()
            .Build();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Infrastructure;

namespace Prepround.Tests.Infrastructure;

public class AddInfrastructureTests
{
    [Fact]
    public void Context_uses_npgsql_with_connection_string_from_configuration()
    {
        // Unresolvable host: creating the context must not connect, so no database is needed here.
        const string connectionString = "Host=db.invalid;Port=5433;Database=prepround;Username=prepround;Password=secret";
        using var provider = BuildProvider(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Prepround"] = connectionString,
        });
        using var scope = provider.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<PreproundContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.Equal(connectionString, context.Database.GetConnectionString());
    }

    [Fact]
    public void Registration_does_not_require_a_connection_string()
    {
        var exception = Record.Exception(() => BuildProvider(new Dictionary<string, string?>()).Dispose());

        Assert.Null(exception);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }
}

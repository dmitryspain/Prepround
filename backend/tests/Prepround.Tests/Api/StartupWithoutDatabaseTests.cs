using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Prepround.Tests.Api;

/// <summary>spec 5.3: the API starts and answers health without a connection string and without a database.</summary>
public class StartupWithoutDatabaseTests
{
    [Fact]
    public async Task Health_is_ok_when_connection_string_is_missing()
    {
        // Overrides user-secrets or environment variables that may exist on the machine (UseSetting stores null as "").
        await using var factory = ApiFactory.Create(settings: new Dictionary<string, string?>
        {
            [ApiFactory.ConnectionStringKey] = null,
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.True(string.IsNullOrEmpty(factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Prepround")));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_ok_when_database_is_unreachable()
    {
        // Port 1 on loopback refuses connections at once: any DB call at startup or in health would fail.
        const string unreachable = "Host=127.0.0.1;Port=1;Database=prepround;Username=prepround;Password=prepround;Timeout=1";
        await using var factory = ApiFactory.Create(settings: new Dictionary<string, string?>
        {
            [ApiFactory.ConnectionStringKey] = unreachable,
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(unreachable, factory.Services.GetRequiredService<IConfiguration>().GetConnectionString("Prepround"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_is_ok_in_production_without_database()
    {
        await using var factory = ApiFactory.Create("Production", new Dictionary<string, string?>
        {
            [ApiFactory.ConnectionStringKey] = null,
        });
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prepround.Tests.Api;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_health_returns_200()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_health_returns_status_ok_json_as_in_spec()
    {
        // spec 5.3: the client relies on the exact field name and value.
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("""{"status":"ok"}""", body);
    }
}

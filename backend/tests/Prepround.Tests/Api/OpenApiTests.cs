using System.Net;

namespace Prepround.Tests.Api;

public class OpenApiTests
{
    [Fact]
    public async Task Document_is_served_in_development_and_describes_health()
    {
        await using var factory = ApiFactory.Create("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"/api/health\"", body);
    }

    [Fact]
    public async Task Document_is_not_served_in_production()
    {
        await using var factory = ApiFactory.Create("Production");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

namespace Prepround.Tests.Api;

public class CorsTests
{
    private const string FrontendDevOrigin = "http://localhost:4300";
    private const string ForeignOrigin = "https://evil.example";

    [Fact]
    public async Task Preflight_from_frontend_dev_origin_is_allowed()
    {
        await using var factory = ApiFactory.Create();
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Preflight(FrontendDevOrigin, "GET"), TestContext.Current.CancellationToken);

        Assert.Equal(FrontendDevOrigin, AllowOrigin(response));
    }

    [Fact]
    public async Task Preflight_from_foreign_origin_gets_no_allow_origin_header()
    {
        await using var factory = ApiFactory.Create();
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Preflight(ForeignOrigin, "GET"), TestContext.Current.CancellationToken);

        Assert.Null(AllowOrigin(response));
    }

    [Fact]
    public async Task Simple_get_from_foreign_origin_gets_no_allow_origin_header()
    {
        await using var factory = ApiFactory.Create();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        request.Headers.Add("Origin", ForeignOrigin);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Null(AllowOrigin(response));
    }

    [Fact]
    public async Task Preflight_for_json_post_from_frontend_dev_origin_allows_post_and_content_type()
    {
        // The test-generation endpoint (spec 5.2) is a JSON POST, so the browser preflights it with Content-Type.
        await using var factory = ApiFactory.Create();
        using var client = factory.CreateClient();
        using var request = Preflight(FrontendDevOrigin, "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(FrontendDevOrigin, AllowOrigin(response));
        Assert.Contains("POST", HeaderValues(response, "Access-Control-Allow-Methods"), StringComparer.OrdinalIgnoreCase);
        Assert.Contains("content-type", HeaderValues(response, "Access-Control-Allow-Headers"), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Allowed_origins_come_only_from_configuration()
    {
        // Replacing the single configured origin must also drop the default one: nothing is hardcoded in code.
        const string configuredOrigin = "https://other.example";
        await using var factory = ApiFactory.Create(settings: new Dictionary<string, string?>
        {
            ["Cors:Origins:0"] = configuredOrigin,
        });
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var configured = await client.SendAsync(Preflight(configuredOrigin, "GET"), ct);
        using var formerDefault = await client.SendAsync(Preflight(FrontendDevOrigin, "GET"), ct);

        Assert.Equal(configuredOrigin, AllowOrigin(configured));
        Assert.Null(AllowOrigin(formerDefault));
    }

    private static HttpRequestMessage Preflight(string origin, string method)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/health");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", method);
        return request;
    }

    private static string? AllowOrigin(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? string.Join(",", values) : null;

    private static string[] HeaderValues(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values)
            ? values.SelectMany(value => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToArray()
            : [];
}

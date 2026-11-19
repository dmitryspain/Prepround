using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prepround.Tests.Api;

/// <summary>
/// PRP-021, ADR 005 sections 2 and 4: behind the Render proxy (and Cloudflare in front of it) every connection comes
/// from a proxy, so the rate limit of <c>POST /api/tests</c> (30 a minute per client IP, spec 5.2) must split by
/// <c>CF-Connecting-IP</c> in Production, and Production CORS allows only the Pages site.
/// An empty body fails on the field before any database call, so no database is needed here.
/// </summary>
public class ForwardedHeadersTests
{
    private const string PagesOrigin = "https://prepround.pages.dev";
    private const string FrontendDevOrigin = "http://localhost:4300";
    private const int PermitLimit = 30;

    // Render's proxy: the only peer the container ever sees.
    private static readonly IPAddress RenderProxy = IPAddress.Parse("10.0.0.1");

    [Fact]
    public async Task Development_without_forwarded_headers_allows_only_the_dev_origin()
    {
        await using var factory = ApiFactory.Create("Development");
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var dev = await client.SendAsync(Preflight(FrontendDevOrigin), ct);
        using var pages = await client.SendAsync(Preflight(PagesOrigin), ct);

        Assert.Equal(FrontendDevOrigin, AllowOrigin(dev));
        Assert.Null(AllowOrigin(pages));
    }

    [Fact]
    public async Task Development_without_forwarded_headers_limits_by_the_connection_ip()
    {
        await using var factory = ApiFactory.Create("Development");
        var first = IPAddress.Parse("203.0.113.1");

        for (var i = 0; i < PermitLimit; i++)
        {
            Assert.Equal(StatusCodes.Status400BadRequest, await PostAsync(factory, first));
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostAsync(factory, first));
        Assert.Equal(StatusCodes.Status400BadRequest, await PostAsync(factory, IPAddress.Parse("203.0.113.2")));
    }

    [Theory]
    [InlineData(PagesOrigin, true)]
    [InlineData(FrontendDevOrigin, false)]
    [InlineData("https://evil.example", false)]
    public async Task Production_cors_allows_only_the_pages_site(string origin, bool allowed)
    {
        await using var factory = ApiFactory.Create("Production");
        using var client = factory.CreateClient();

        using var response = await client.SendAsync(Preflight(origin), TestContext.Current.CancellationToken);

        Assert.Equal(allowed ? origin : null, AllowOrigin(response));
    }

    [Fact]
    public async Task Production_limit_splits_by_cf_connecting_ip_whatever_the_proxy_and_x_forwarded_for()
    {
        // Spoofed X-Forwarded-For values and different Render proxies must not open new partitions for one client.
        await using var factory = ApiFactory.Create("Production");

        for (var i = 0; i < PermitLimit; i++)
        {
            var status = await PostAsync(factory, IPAddress.Parse($"10.0.0.{i + 1}"), new()
            {
                ["CF-Connecting-IP"] = "203.0.113.1",
                ["X-Forwarded-For"] = $"198.51.100.{i}, 203.0.113.1, 104.16.0.1",
            });
            Assert.Equal(StatusCodes.Status400BadRequest, status);
        }

        var thirtyFirst = await PostAsync(factory, RenderProxy, new()
        {
            ["CF-Connecting-IP"] = "203.0.113.1",
            ["X-Forwarded-For"] = "198.51.100.200",
        });
        var otherClient = await PostAsync(factory, RenderProxy, new() { ["CF-Connecting-IP"] = "203.0.113.2" });

        Assert.Equal(StatusCodes.Status429TooManyRequests, thirtyFirst);
        Assert.Equal(StatusCodes.Status400BadRequest, otherClient);
    }

    [Fact]
    public async Task Production_ignores_x_forwarded_for_so_it_cannot_dodge_the_limit()
    {
        // Without CF-Connecting-IP every request stays on the proxy's own address, whatever X-Forwarded-For says.
        await using var factory = ApiFactory.Create("Production");

        for (var i = 0; i < PermitLimit; i++)
        {
            var status = await PostAsync(factory, RenderProxy, new() { ["X-Forwarded-For"] = $"198.51.100.{i}" });
            Assert.Equal(StatusCodes.Status400BadRequest, status);
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests,
            await PostAsync(factory, RenderProxy, new() { ["X-Forwarded-For"] = "198.51.100.250" }));
    }

    [Fact]
    public async Task Fallback_settings_split_the_limit_by_the_second_x_forwarded_for_entry_from_the_right()
    {
        // ADR 005, section 4: X-Forwarded-For with ForwardLimit 2, switched on by settings without a new build.
        await using var factory = ApiFactory.Create("Production", new Dictionary<string, string?>
        {
            ["ForwardedHeaders:ForwardedForHeaderName"] = "X-Forwarded-For",
            ["ForwardedHeaders:ForwardLimit"] = "2",
        });

        for (var i = 0; i < PermitLimit; i++)
        {
            var status = await PostAsync(factory, RenderProxy, new()
            {
                ["X-Forwarded-For"] = $"198.51.100.{i}, 203.0.113.7, 104.16.0.1",
                ["CF-Connecting-IP"] = $"192.0.2.{i}",
            });
            Assert.Equal(StatusCodes.Status400BadRequest, status);
        }

        var thirtyFirst = await PostAsync(factory, RenderProxy, new() { ["X-Forwarded-For"] = "198.51.100.99, 203.0.113.7, 104.16.0.1" });
        var otherClient = await PostAsync(factory, RenderProxy, new() { ["X-Forwarded-For"] = "198.51.100.99, 203.0.113.8, 104.16.0.1" });

        Assert.Equal(StatusCodes.Status429TooManyRequests, thirtyFirst);
        Assert.Equal(StatusCodes.Status400BadRequest, otherClient);
    }

    private static async Task<int> PostAsync(
        WebApplicationFactory<Program> factory, IPAddress peer, Dictionary<string, string>? headers = null)
    {
        var context = await factory.Server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Path = "/api/tests";
            http.Request.ContentType = "application/json";
            http.Request.Body = new MemoryStream("{}"u8.ToArray());
            http.Connection.RemoteIpAddress = peer;
            foreach (var (name, value) in headers ?? [])
            {
                http.Request.Headers[name] = value;
            }
        }, TestContext.Current.CancellationToken);
        return context.Response.StatusCode;
    }

    private static HttpRequestMessage Preflight(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/tests");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }

    private static string? AllowOrigin(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? string.Join(",", values) : null;
}

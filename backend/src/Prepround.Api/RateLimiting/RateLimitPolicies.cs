using System.Threading.RateLimiting;

namespace Prepround.Api.RateLimiting;

/// <summary>Named rate-limit policies; endpoints opt in with <c>[EnableRateLimiting]</c>, the rest are not limited.</summary>
public static class RateLimitPolicies
{
    /// <summary><c>POST /api/tests</c>: 30 requests a minute per client IP (spec 5.2).</summary>
    public const string Tests = "tests";

    private const int TestsPermitLimit = 30;
    private static readonly TimeSpan TestsWindow = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddRateLimitPolicies(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // No queue: a request over the limit is rejected at once instead of waiting for the next window.
            options.AddPolicy(Tests, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = TestsPermitLimit,
                    Window = TestsWindow,
                    QueueLimit = 0,
                }));
        });
}

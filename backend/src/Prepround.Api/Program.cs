using Microsoft.AspNetCore.HttpOverrides;
using Prepround.Api.RateLimiting;
using Prepround.Application;
using Prepround.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

// Allowed origins come only from configuration (Cors:Origins); an empty list allows none.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyMethod()
    .AllowAnyHeader()));

// Behind the Render proxy (and Cloudflare in front of it) the connection comes from a proxy, so the client IP
// for the rate limit (spec 5.2) and the scheme come from forwarded headers (ADR 005, section 4).
var forwardedHeaders = builder.Configuration.GetSection("ForwardedHeaders");
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Render does not publish its proxy IPs, and the container is reachable only through them, so every
    // hop is trusted. KnownIPNetworks, not KnownNetworks: the latter is obsolete in .NET 10 (ASPDEPR005).
    options.KnownProxies.Clear();
    options.KnownIPNetworks.Clear();
    // Configurable so a Render variable can switch the header or the limit without a new build;
    // without the section the defaults stay (X-Forwarded-For, 1).
    if (forwardedHeaders["ForwardedForHeaderName"] is { Length: > 0 } headerName)
    {
        options.ForwardedForHeaderName = headerName;
    }

    options.ForwardLimit = forwardedHeaders.GetValue("ForwardLimit", options.ForwardLimit);
});

builder.Services.AddRateLimitPolicies();

var app = builder.Build();

// First in the pipeline: HTTPS redirection, CORS and the rate limiter must see the client's scheme and IP.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
// After CORS: the CORS middleware answers preflight requests itself, so they never spend rate-limit permits,
// and a 429 still carries Access-Control-Allow-Origin, so the browser lets the frontend read it.
app.UseRateLimiter();
app.MapControllers();

app.Run();

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

builder.Services.AddRateLimitPolicies();

var app = builder.Build();

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

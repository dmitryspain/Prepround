using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Prepround.Tests.Api;

/// <summary>
/// Builds an in-memory API with a chosen environment and settings. UseSetting is used instead of
/// ConfigureAppConfiguration because Program.cs reads configuration (Cors:Origins) before Build().
/// </summary>
internal static class ApiFactory
{
    public const string ConnectionStringKey = "ConnectionStrings:Prepround";

    public static WebApplicationFactory<Program> Create(
        string environment = "Development",
        IReadOnlyDictionary<string, string?>? settings = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);

            foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            {
                builder.UseSetting(key, value);
            }
        });
}

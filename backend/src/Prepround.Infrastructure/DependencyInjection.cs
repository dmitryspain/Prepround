using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application.Catalog;
using Prepround.Application.Content;
using Prepround.Application.Content.Import;
using Prepround.Application.TestGeneration;
using Prepround.Infrastructure.Content;
using Prepround.Infrastructure.Repositories;

namespace Prepround.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Key under ConnectionStrings; the value lives only in user-secrets or environment variables.</summary>
    public const string ConnectionStringName = "Prepround";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // The options callback runs on first resolve of the context, not at startup,
        // so the API (and /api/health) starts without a connection string or a running database.
        services.AddDbContext<PreproundContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString(ConnectionStringName)));

        // Stateless, so one reader serves every scope; validate resolves it without touching the database.
        services.AddSingleton<IContentReader, YamlContentReader>();

        // Scoped like the context it wraps.
        services.AddScoped<ICatalogImportRepository, CatalogImportRepository>();
        services.AddScoped<ICatalogReadRepository, CatalogReadRepository>();
        services.AddScoped<ITestGenerationRepository, TestGenerationRepository>();

        return services;
    }
}

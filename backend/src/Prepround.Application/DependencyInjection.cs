using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content;

namespace Prepround.Application;

public static class DependencyInjection
{
    private static readonly Type[] HandlerInterfaces = [typeof(IQueryHandler<,>), typeof(ICommandHandler<,>)];

    /// <summary>
    /// Registers every query and command handler in this assembly, so a new feature needs no DI edits.
    /// Scoped because handlers depend on the scoped DbContext.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var registrations = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType
                                   && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Service: contract, Implementation: type)));

        foreach (var (service, implementation) in registrations)
        {
            services.AddScoped(service, implementation);
        }

        // The content read-and-check step holds no state, so one instance serves every scope.
        services.AddSingleton<ContentValidator>();
        services.AddSingleton<ContentLoader>();

        // The clock is a seam for tests (UpdatedAt of questions); TryAdd keeps a TimeProvider registered earlier.
        services.TryAddSingleton(TimeProvider.System);

        // The randomness seam of test generation (QuestionPicker): Random.Shared is thread-safe, so one instance
        // serves concurrent requests. Tests register a seeded Random; TryAdd keeps one registered earlier.
        services.TryAddSingleton(Random.Shared);

        return services;
    }
}

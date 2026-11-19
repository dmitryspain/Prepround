using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Infrastructure;

namespace Prepround.Tests.Architecture;

/// <summary>
/// The composition used by the API and ContentTool builds with full validation, and every handler resolves in a scope.
/// Handlers are found by reflection, so handlers of later cards are covered without touching this test.
/// </summary>
public class HandlerCompositionTests
{
    private static readonly Type[] HandlerInterfaces = [typeof(IQueryHandler<,>), typeof(ICommandHandler<,>)];

    [Theory]
    // Unresolvable host: resolving must not connect.
    [InlineData("Host=db.invalid;Port=5433;Database=prepround;Username=prepround;Password=secret")]
    // validate runs this composition without any connection string.
    [InlineData(null)]
    public void Every_handler_in_the_application_assembly_is_registered_and_resolves_in_a_scope(string? connectionString)
    {
        var handlers = typeof(IQuery<>).Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && HandlerInterfaces.Contains(contract.GetGenericTypeDefinition()))
                .Select(contract => (Service: contract, Implementation: type)))
            .ToList();
        var services = Compose(connectionString);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotEmpty(handlers);
        Assert.All(handlers, handler => Assert.IsType(handler.Implementation, scope.ServiceProvider.GetRequiredService(handler.Service)));
    }

    [Fact]
    public void Every_registered_handler_resolves_in_a_scope_but_not_from_the_root()
    {
        var services = Compose("Host=db.invalid;Port=5433;Database=prepround;Username=prepround;Password=secret");
        var registered = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType
                                 && HandlerInterfaces.Contains(descriptor.ServiceType.GetGenericTypeDefinition()))
            .ToList();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotEmpty(registered);
        Assert.All(registered, descriptor =>
        {
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
            Assert.NotNull(scope.ServiceProvider.GetRequiredService(descriptor.ServiceType));
            Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService(descriptor.ServiceType));
        });
    }

    private static ServiceCollection Compose(string? connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Prepround"] = connectionString })
            .Build();
        var services = new ServiceCollection();
        services.AddApplication().AddInfrastructure(configuration);
        return services;
    }
}

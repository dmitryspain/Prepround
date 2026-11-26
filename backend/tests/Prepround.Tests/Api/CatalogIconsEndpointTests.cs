using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content.Import;
using Prepround.Infrastructure;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.Api;

/// <summary>
/// <c>icon</c> and <c>symbol</c> in <c>GET /api/catalog</c> (spec 5.1, PRP-033) of the real content: the values of the
/// card, <c>null</c> rather than an absent property, and the same in every language.
/// </summary>
[Collection(PostgresCollection.Name)]
public class CatalogIconsEndpointTests(PostgresFixture postgres)
{
    /// <summary>The card's table, written out independently of the content files.</summary>
    private static readonly Dictionary<string, (string Icon, string Symbol)> CardStacks = new()
    {
        ["dotnet"] = ("dotnet", "Nt"), ["java"] = ("openjdk", "Jv"), ["python"] = ("python", "Py"),
        ["nodejs"] = ("nodedotjs", "Nd"), ["go"] = ("go", "Go"), ["php"] = ("php", "Ph"),
        ["react"] = ("react", "Re"), ["angular"] = ("angular", "Ng"), ["vue"] = ("vuedotjs", "Vu"),
    };

    /// <summary>Topics with a logo by the card; every other topic has none.</summary>
    private static readonly Dictionary<string, string> CardTopicIcons = new()
    {
        ["aspnet-core"] = "dotnet", ["sql"] = "postgresql", ["messaging"] = "rabbitmq", ["docker-k8s"] = "kubernetes",
        ["java-core"] = "openjdk", ["spring-boot"] = "springboot", ["hibernate-jpa"] = "hibernate",
        ["aws"] = "amazonwebservices", ["python-core"] = "python", ["django"] = "django", ["fastapi"] = "fastapi",
        ["sqlalchemy"] = "sqlalchemy", ["celery"] = "celery", ["pytest"] = "pytest", ["nosql"] = "redis",
        ["javascript"] = "javascript", ["typescript"] = "typescript", ["nodejs-runtime"] = "nodedotjs",
        ["express-fastify"] = "fastify", ["nestjs"] = "nestjs", ["prisma-typeorm"] = "prisma", ["api-design"] = "swagger",
        ["go-lang"] = "go", ["go-db"] = "postgresql", ["php-lang"] = "php", ["laravel"] = "laravel",
        ["symfony"] = "symfony", ["composer-psr"] = "composer", ["react-core"] = "react", ["react-hooks"] = "react",
        ["nextjs"] = "nextdotjs", ["angular-components"] = "angular", ["angular-di"] = "angular",
        ["angular-change-detection"] = "angular", ["angular-routing-forms"] = "angular", ["vue-core"] = "vuedotjs",
        ["vue-composition-api"] = "vuedotjs", ["vue-router"] = "vuedotjs", ["html-css"] = "html5",
        ["frontend-testing"] = "jest",
    };

    [Fact]
    public async Task Stacks_and_topics_carry_the_card_icons_and_symbols_null_when_absent_in_both_languages()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportAsync(db, RealContentDirectory());

        foreach (var language in new[] { "uk", "en" })
        {
            var field = Assert.Single((await GetCatalogAsync(db, language))["fields"]!.AsArray())!.AsObject();

            var stacks = field["directions"]!.AsArray().SelectMany(direction => direction!["stacks"]!.AsArray())
                .Select(stack => stack!.AsObject()).ToList();
            Assert.Equal(CardStacks.Keys.Order(), stacks.Select(stack => Str(stack, "id")).Order());
            Assert.All(stacks, stack => Assert.Equal(CardStacks[Str(stack, "id")], (Str(stack, "icon"), Str(stack, "symbol"))));

            var topics = field["topics"]!.AsArray().Select(topic => topic!.AsObject()).ToList();
            Assert.All(topics, topic =>
            {
                // Present and null, not left out: the client tells "no logo" from an older API by the value only.
                Assert.True(topic.ContainsKey("icon"), Str(topic, "id"));
                var icon = topic["icon"]?.GetValue<string>();
                Assert.Equal(CardTopicIcons.GetValueOrDefault(Str(topic, "id")), icon);
            });
            Assert.Equal(CardTopicIcons.Count, topics.Count(topic => topic["icon"] is not null));
        }
    }

    private static async Task ImportAsync(string connectionString, string contentRoot)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ApiFactory.ConnectionStringKey] = connectionString })
            .Build();
        var services = new ServiceCollection();
        services.AddApplication().AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportContentCommand, ImportSummary>>();

        var result = await handler.Handle(new ImportContentCommand(contentRoot, false), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    private static async Task<JsonObject> GetCatalogAsync(string connectionString, string language)
    {
        await using var factory = ApiFactory.Create(settings: new Dictionary<string, string?>
        {
            [ApiFactory.ConnectionStringKey] = connectionString,
        });
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.GetAsync($"/api/catalog?lang={language}", ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return JsonNode.Parse(text)!.AsObject();
    }

    private static string Str(JsonObject node, string name) => node[name]!.GetValue<string>();
}

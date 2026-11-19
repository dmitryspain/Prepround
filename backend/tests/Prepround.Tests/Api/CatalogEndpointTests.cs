using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content;
using Prepround.Application.Content.Import;
using Prepround.Infrastructure;
using Prepround.Tests.ContentTool;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.Api;

/// <summary>
/// <c>GET /api/catalog</c> (spec 5.1, PRP-005) through the real API (WebApplicationFactory) on a Testcontainers Postgres
/// filled by the real import. Deactivations the import cannot produce are made by direct SQL.
/// </summary>
[Collection(PostgresCollection.Name)]
public class CatalogEndpointTests(PostgresFixture postgres)
{
    private const string FrontendDevOrigin = "http://localhost:4300";
    private const string FieldFile = "it/field.yaml";
    private const string EfCoreFile = "it/topics/ef-core.yaml";
    private const string AzureFile = "it/topics/azure.yaml";
    private const string SqlFile = "it/topics/sql.yaml";

    /// <summary>spec 4.1, written out independently of field.yaml: stacks in table order, topics in row order.</summary>
    private static readonly (string Direction, string Stack, string[] TopicIds)[] Spec41 =
    [
        ("backend", "dotnet", ["csharp", "dotnet-clr", "dotnet-async", "aspnet-core", "ef-core", "sql", "architecture", "messaging", "azure", "docker-k8s"]),
        ("backend", "java", ["java-core", "jvm", "java-concurrency", "spring-boot", "hibernate-jpa", "sql", "architecture", "messaging", "aws", "docker-k8s"]),
        ("backend", "python", ["python-core", "python-async", "django", "fastapi", "sqlalchemy", "celery", "pytest", "sql", "nosql", "docker-k8s"]),
        ("backend", "nodejs", ["javascript", "typescript", "nodejs-runtime", "express-fastify", "nestjs", "prisma-typeorm", "sql", "nosql", "api-design", "docker-k8s"]),
        ("backend", "go", ["go-lang", "go-concurrency", "go-runtime", "go-http", "go-db", "api-design", "sql", "messaging", "architecture", "docker-k8s"]),
        ("backend", "php", ["php-lang", "laravel", "symfony", "php-orm", "composer-psr", "phpunit", "sql", "nosql", "api-design", "architecture"]),
        ("frontend", "react", ["javascript", "typescript", "html-css", "react-core", "react-hooks", "react-state", "nextjs", "react-data-routing", "frontend-testing", "web-performance"]),
        ("frontend", "angular", ["javascript", "typescript", "html-css", "angular-components", "angular-di", "angular-change-detection", "rxjs", "ngrx", "angular-routing-forms", "frontend-testing"]),
        ("frontend", "vue", ["javascript", "typescript", "html-css", "vue-core", "vue-composition-api", "pinia", "vue-router", "nuxt", "frontend-testing", "web-performance"]),
    ];

    [Fact]
    public async Task Catalog_of_the_real_content_has_the_shape_and_order_of_spec_5_1()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportAsync(db, RealContentDirectory());

        var (response, body) = await GetCatalogAsync(db);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["fields"], Names(body));
        var field = Assert.Single(body["fields"]!.AsArray())!.AsObject();
        Assert.Equal(["id", "title", "levels", "directions", "topics"], Names(field));
        Assert.Equal(("it", "IT"), (Str(field, "id"), Str(field, "title")));

        var levels = field["levels"]!.AsArray().Select(level => level!.AsObject()).ToList();
        Assert.All(levels, level => Assert.Equal(["id", "title", "rank"], Names(level)));
        Assert.Equal(
            [("junior", "Junior", 1), ("middle", "Middle", 2), ("senior", "Senior", 3)],
            levels.Select(level => (Str(level, "id"), Str(level, "title"), level["rank"]!.GetValue<int>())));

        var directions = field["directions"]!.AsArray().Select(direction => direction!.AsObject()).ToList();
        Assert.All(directions, direction => Assert.Equal(["id", "title", "stacks"], Names(direction)));
        Assert.Equal([("backend", "Backend"), ("frontend", "Frontend")], directions.Select(d => (Str(d, "id"), Str(d, "title"))));
        var stacks = directions
            .SelectMany(direction => direction["stacks"]!.AsArray().Select(stack => (Direction: Str(direction, "id"), Stack: stack!.AsObject())))
            .ToList();
        Assert.All(stacks, stack => Assert.Equal(["id", "title", "topicIds"], Names(stack.Stack)));
        Assert.Equal(
            Spec41.Select(row => $"{row.Direction}/{row.Stack}: {string.Join(",", row.TopicIds)}"),
            stacks.Select(s => $"{s.Direction}/{Str(s.Stack, "id")}: {string.Join(",", s.Stack["topicIds"]!.AsArray().Select(id => id!.GetValue<string>()))}"));
        Assert.Equal(".NET (C#)", Str(stacks[0].Stack, "title"));

        var topics = field["topics"]!.AsArray().Select(topic => topic!.AsObject()).ToList();
        Assert.Equal(61, topics.Count);
        Assert.All(topics, topic => Assert.Equal(["id", "title", "description", "questionCounts"], Names(topic)));
        var titles = topics.Select(topic => Str(topic, "title")).ToList();
        Assert.Equal(titles.Order(StringComparer.InvariantCulture), titles);
        // Every topic a stack refers to is in the field's topic list.
        var topicIds = topics.Select(topic => Str(topic, "id")).ToHashSet();
        Assert.Subset(topicIds, Spec41.SelectMany(row => row.TopicIds).ToHashSet());

        // Titles, descriptions and counts per level come from the files: the files are the oracle here, since content
        // waves keep filling topics (ADR 004). Counts are compared as exact JSON, so the key order (scale order) and
        // "{}" for a topic without questions (if the content still has one) are checked for every topic.
        var file = RealField();
        var expected = file.Topics.ToDictionary(topic => topic.Id);
        Assert.Equal(expected.Keys.Order(), topicIds.Order());
        Assert.All(topics, topic =>
        {
            var fromFile = expected[Str(topic, "id")];
            Assert.Equal((fromFile.Title, fromFile.Description), (Str(topic, "title"), Str(topic, "description")));
            Assert.Equal(CountsJson(file.Levels, fromFile.Questions), topic["questionCounts"]!.ToJsonString());
        });
        // At least one topic has questions on every level, so the key order above is really checked.
        Assert.Contains(topics, topic => topic["questionCounts"]!.AsObject().Count == levels.Count);
    }

    [Fact]
    public async Task Topic_without_questions_has_empty_question_counts_and_stays_in_the_catalog()
    {
        // The real content fills every topic over time (ADR 004), so the empty topic is made in a copy:
        // azure keeps its header and its place in the dotnet stack, but loses all questions.
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        var text = content.Read(AzureFile);
        var questions = text.IndexOf("\nquestions:", StringComparison.Ordinal);
        Assert.True(questions > 0, $"{AzureFile} has no \"questions:\" key.");
        content.Write(AzureFile, text[..questions] + "\nquestions: []\n");
        await ImportAsync(db, content.Root);

        var (_, body) = await GetCatalogAsync(db);

        var azure = Topic(body, "azure");
        Assert.Equal("{}", azure["questionCounts"]!.ToJsonString());
        var fromFile = RealTopic("azure");
        Assert.Equal((fromFile.Title, fromFile.Description), (Str(azure, "title"), Str(azure, "description")));
        Assert.Contains("azure", Stack(body, "dotnet")["topicIds"]!.AsArray().Select(id => id!.GetValue<string>()));
        // Only azure was emptied: the other topics keep the counts of their files.
        Assert.Equal(CountsJson(RealField().Levels, RealTopic("ef-core").Questions), Topic(body, "ef-core")["questionCounts"]!.ToJsonString());
    }

    [Fact]
    public async Task Catalog_answers_the_frontend_dev_origin_with_allow_origin()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportAsync(db, RealContentDirectory());

        var (response, _) = await GetCatalogAsync(db, FrontendDevOrigin);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal([FrontendDevOrigin], values);
    }

    [Fact]
    public async Task Question_removed_from_the_file_is_no_longer_counted_after_import()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportAsync(db, content.Root);

        // Drop the first junior question of ef-core: everything from its "- id:" up to the next one.
        var text = content.Read(EfCoreFile);
        var start = text.IndexOf("  - id: ef-core-001", StringComparison.Ordinal);
        var next = text.IndexOf("  - id: ", start + 1, StringComparison.Ordinal);
        Assert.True(start >= 0 && next > start, "ef-core.yaml no longer starts with ef-core-001.");
        Assert.Contains("level: junior", text[start..next]);
        content.Write(EfCoreFile, text[..start] + text[next..]);
        await ImportAsync(db, content.Root);

        var (_, body) = await GetCatalogAsync(db);

        var expected = CountsJson(RealField().Levels, RealTopic("ef-core").Questions.Where(question => question.Id != "ef-core-001"));
        Assert.Equal(expected, Topic(body, "ef-core")["questionCounts"]!.ToJsonString());
    }

    [Fact]
    public async Task Inactive_questions_are_not_counted_and_a_level_left_without_questions_is_absent()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportAsync(db, RealContentDirectory());

        await PostgresFixture.ExecuteAsync(db, """UPDATE "Questions" SET "IsActive" = false WHERE "Id" = 'ef-core-001'""");
        var (_, one) = await GetCatalogAsync(db);
        await PostgresFixture.ExecuteAsync(db,
            """UPDATE "Questions" SET "IsActive" = false WHERE "TopicId" = 'ef-core' AND "LevelId" = 'junior'""");
        var (_, allJunior) = await GetCatalogAsync(db);

        var scale = RealField().Levels;
        var efCore = RealTopic("ef-core").Questions;
        Assert.Contains(efCore, question => question is { Id: "ef-core-001", LevelId: "junior" });
        Assert.Contains(efCore, question => question.LevelId != "junior");
        Assert.Equal(
            CountsJson(scale, efCore.Where(question => question.Id != "ef-core-001")),
            Topic(one, "ef-core")["questionCounts"]!.ToJsonString());
        Assert.Equal(
            CountsJson(scale, efCore.Where(question => question.LevelId != "junior")),
            Topic(allJunior, "ef-core")["questionCounts"]!.ToJsonString());
        Assert.DoesNotContain("junior", Topic(allJunior, "ef-core")["questionCounts"]!.AsObject().Select(pair => pair.Key));
        // The level itself stays in the scale: only the topic has no junior questions left.
        Assert.Contains("junior", Field(allJunior)["levels"]!.AsArray().Select(level => Str(level!.AsObject(), "id")));
    }

    [Fact]
    public async Task Inactive_topic_is_in_neither_topics_nor_topicIds_even_with_an_active_stack_link()
    {
        // The import cannot leave an active stack link to an inactive topic (the validator forbids it), so SQL does it.
        var db = await postgres.CreateDatabaseAsync();
        await ImportAsync(db, RealContentDirectory());
        await PostgresFixture.ExecuteAsync(db, """UPDATE "Topics" SET "IsActive" = false WHERE "Id" = 'azure'""");

        var (_, body) = await GetCatalogAsync(db);

        var topicIds = Field(body)["topics"]!.AsArray().Select(topic => Str(topic!.AsObject(), "id")).ToList();
        Assert.Equal(60, topicIds.Count);
        Assert.DoesNotContain("azure", topicIds);
        var dotnet = Stack(body, "dotnet")["topicIds"]!.AsArray().Select(id => id!.GetValue<string>()).ToList();
        Assert.Equal(Spec41[0].TopicIds.Where(id => id != "azure"), dotnet);
        Assert.DoesNotContain(
            "azure",
            Field(body)["directions"]!.AsArray()
                .SelectMany(direction => direction!["stacks"]!.AsArray())
                .SelectMany(stack => stack!["topicIds"]!.AsArray().Select(id => id!.GetValue<string>())));
    }

    [Fact]
    public async Task Level_added_to_a_content_copy_appears_in_levels_and_question_counts_without_code_changes()
    {
        var db = await postgres.CreateDatabaseAsync();
        var questionId = FreeQuestionId("sql");
        using var content = new RealContentCopy();
        content.Replace(FieldFile, "  - id: senior\n    title: Senior", """
              - id: senior
                title: Senior
              - id: lead-architect
                title: Lead Architect
              - id: principal
                title: Principal
            """);
        content.Write(SqlFile, content.Read(SqlFile) + $"""

              - id: {questionId}
                level: lead-architect
                type: single
                text: Питання для рівня, якого в коді немає?
                options:
                  - text: Так
                    correct: true
                  - text: Ні
                explanation: Рівні — це дані з field.yaml.
            """);
        await ImportAsync(db, content.Root);

        var (_, body) = await GetCatalogAsync(db);

        Assert.Equal(
            [("junior", 1), ("middle", 2), ("senior", 3), ("lead-architect", 4), ("principal", 5)],
            Field(body)["levels"]!.AsArray().Select(level => (Str(level!.AsObject(), "id"), level!["rank"]!.GetValue<int>())));
        Assert.Equal("Lead Architect", Str(Field(body)["levels"]![3]!.AsObject(), "title"));
        // The key is the level id as written in the file (no naming policy), in scale order; "principal" has no questions.
        // The new level sorts between junior and middle by name, so the scale order is really checked here.
        var scale = RealField().Levels.Select(level => level.Id).Concat(["lead-architect", "principal"]);
        var levelIds = RealTopic("sql").Questions.Select(question => question.LevelId).Append("lead-architect");
        Assert.Equal(CountsJson(scale, levelIds), Topic(body, "sql")["questionCounts"]!.ToJsonString());
        Assert.EndsWith(""","lead-architect":1}""", Topic(body, "sql")["questionCounts"]!.ToJsonString());
        Assert.DoesNotContain(
            Field(body)["topics"]!.AsArray(),
            topic => topic!["questionCounts"]!.AsObject().ContainsKey("principal"));
    }

    /// <summary>Imports in-process through the same composition as ContentTool, and fails the test unless it succeeds.</summary>
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

        var result = await handler.Handle(new ImportContentCommand(contentRoot), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
    }

    private static async Task<(HttpResponseMessage Response, JsonObject Body)> GetCatalogAsync(string connectionString, string? origin = null)
    {
        await using var factory = ApiFactory.Create(settings: new Dictionary<string, string?>
        {
            [ApiFactory.ConnectionStringKey] = connectionString,
        });
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/catalog");
        if (origin is not null)
        {
            request.Headers.Add("Origin", origin);
        }

        var ct = TestContext.Current.CancellationToken;
        var response = await client.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, text);
        return (response, JsonNode.Parse(text)!.AsObject());
    }

    /// <summary>
    /// <c>questionCounts</c> as spec 5.1 writes it, built by hand rather than by the API's serializer: levels in scale
    /// order, a level without questions left out, <c>{}</c> for none.
    /// </summary>
    private static string CountsJson(IEnumerable<string> scale, IEnumerable<string> questionLevelIds)
    {
        var counts = questionLevelIds.CountBy(levelId => levelId).ToDictionary();
        var order = scale.ToList();
        Assert.Subset(order.ToHashSet(), counts.Keys.ToHashSet());
        return "{" + string.Join(",", order.Where(counts.ContainsKey).Select(levelId => $"\"{levelId}\":{counts[levelId]}")) + "}";
    }

    private static string CountsJson(IEnumerable<LevelDefinition> scale, IEnumerable<QuestionDefinition> questions) =>
        CountsJson(scale.Select(level => level.Id), questions.Select(question => question.LevelId));

    private static string[] Names(JsonObject node) => node.Select(pair => pair.Key).ToArray();

    private static string Str(JsonObject node, string name) => node[name]!.GetValue<string>();

    private static JsonObject Field(JsonObject body) => Assert.Single(body["fields"]!.AsArray())!.AsObject();

    private static JsonObject Topic(JsonObject body, string id) =>
        Assert.Single(Field(body)["topics"]!.AsArray(), topic => Str(topic!.AsObject(), "id") == id)!.AsObject();

    private static JsonObject Stack(JsonObject body, string id) =>
        Assert.Single(
            Field(body)["directions"]!.AsArray().SelectMany(direction => direction!["stacks"]!.AsArray()),
            stack => Str(stack!.AsObject(), "id") == id)!.AsObject();
}

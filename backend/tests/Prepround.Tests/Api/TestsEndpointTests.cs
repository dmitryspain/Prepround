using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content;
using Prepround.Application.Content.Import;
using Prepround.Infrastructure;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.Api;

/// <summary>
/// <c>POST /api/tests</c> (spec 5.2, PRP-006) through the real API (WebApplicationFactory) on a Testcontainers Postgres
/// filled by the real import of the seed content. Randomness is a seeded Random put into DI, so every run is the same.
/// The rate limiter lives in the host, so every test builds its own factory and stays under 30 POSTs unless it tests the limit.
/// </summary>
[Collection(PostgresCollection.Name)]
public class TestsEndpointTests(PostgresFixture postgres)
{
    private const string FrontendDevOrigin = "http://localhost:4300";
    private static readonly string[] AllLevels = ["junior", "middle", "senior"];
    private static readonly string[] SeedTopics = ["csharp", "ef-core", "sql"];

    [Fact]
    public async Task Valid_request_gives_5_questions_of_each_topic_on_the_selected_levels_only()
    {
        var db = await SeededDatabaseAsync();
        await using var factory = Factory(db, seed: 1);
        using var client = factory.CreateClient();

        var (status, body) = await PostAsync(client, Request(["middle", "senior"], ("ef-core", 5), ("sql", 5)));

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(["questions"], Names(body));
        var questions = Questions(body);
        Assert.Equal(10, questions.Count);
        Assert.Equal(10, questions.Select(question => Str(question, "id")).Distinct().Count());
        Assert.Equal(5, questions.Count(question => Str(question, "topicId") == "ef-core"));
        Assert.Equal(5, questions.Count(question => Str(question, "topicId") == "sql"));
        Assert.All(questions, question =>
        {
            Assert.Equal(["id", "topicId", "levelId", "type", "text", "code", "options", "explanation"], Names(question));
            Assert.Contains(Str(question, "levelId"), (string[])["middle", "senior"]);
            Assert.Contains(Str(question, "type"), (string[])["single", "multiple"]);
            Assert.False(string.IsNullOrWhiteSpace(Str(question, "explanation")));
            Assert.All(question["options"]!.AsArray(), option =>
            {
                Assert.Equal(["key", "text", "isCorrect"], Names(option!.AsObject()));
                Assert.Contains(option["isCorrect"]!.GetValueKind(), (JsonValueKind[])[JsonValueKind.True, JsonValueKind.False]);
            });
        });

        // Questions of the two topics are mixed, not ef-core first and sql after it.
        var topicsInOrder = questions.Select(question => Str(question, "topicId")).ToList();
        Assert.True(topicsInOrder.Zip(topicsInOrder.Skip(1)).Count(pair => pair.First != pair.Second) > 1, string.Join(",", topicsInOrder));
    }

    [Fact]
    public async Task Options_are_shuffled_and_every_key_is_the_index_of_the_option_in_the_file()
    {
        var db = await SeededDatabaseAsync();
        // Only the topics requested below: the real content has more topics since PRP-018. Every question of each is
        // requested, counted from the files, since content waves keep deepening topics (ADR 004).
        var seedTopics = RealField().Topics.Where(topic => SeedTopics.Contains(topic.Id)).ToList();
        var files = seedTopics.SelectMany(topic => topic.Questions).ToDictionary(question => question.Id);
        await using var factory = Factory(db, seed: 7);
        using var client = factory.CreateClient();

        var (status, body) = await PostAsync(client, Request(AllLevels, seedTopics.Select(topic => (topic.Id, topic.Questions.Count)).ToArray()));

        Assert.Equal(HttpStatusCode.OK, status);
        var questions = Questions(body);
        Assert.Equal(files.Count, questions.Count);
        Assert.Equal(files.Keys.Order(StringComparer.Ordinal), questions.Select(question => Str(question, "id")).Order(StringComparer.Ordinal));

        // In the seed every correct option is written first, so without the shuffle no option 0 would ever be wrong.
        Assert.Contains(questions, question => !question["options"]![0]!["isCorrect"]!.GetValue<bool>());
        Assert.Contains(questions, question =>
            !Keys(question).SequenceEqual(Enumerable.Range(0, Keys(question).Count)));
        Assert.All(questions, question =>
        {
            var file = files[Str(question, "id")];
            Assert.Equal(Enumerable.Range(0, file.Options.Count), Keys(question).Order());
            Assert.All(question["options"]!.AsArray(), option =>
            {
                var fromFile = file.Options[option!["key"]!.GetValue<int>()];
                Assert.Equal((fromFile.Text, fromFile.IsCorrect), (Str(option.AsObject(), "text"), option["isCorrect"]!.GetValue<bool>()));
            });
            Assert.Equal((file.Text, file.Explanation, file.LevelId, file.Type), (Str(question, "text"), Str(question, "explanation"), Str(question, "levelId"), Str(question, "type")));
            AssertCode(file.Code, question["code"]);
        });
    }

    [Fact]
    public async Task Different_seeds_give_different_orders_of_the_same_questions_and_one_seed_repeats_itself()
    {
        var db = await SeededDatabaseAsync();
        // Whole topics, so both seeds pick the same questions whatever the files hold and differ only in order.
        var request = Request(
            AllLevels,
            ("csharp", RealTopic("csharp").Questions.Count),
            ("ef-core", RealTopic("ef-core").Questions.Count),
            ("sql", RealTopic("sql").Questions.Count));

        var first = await OrderAsync(db, seed: 1, request);
        var again = await OrderAsync(db, seed: 1, request);
        var second = await OrderAsync(db, seed: 2, request);

        Assert.Equal(first, again);
        Assert.NotEqual(first, second);
        Assert.Equal(first.Select(item => item.Id).Order(), second.Select(item => item.Id).Order());
        Assert.NotEqual(first.Select(item => item.Id), second.Select(item => item.Id));
    }

    [Fact]
    public async Task Excluded_questions_fill_up_only_what_the_unseen_ones_lack()
    {
        // Of sql and ef-core only the first 4 questions of each file stay unseen, the rest (however many the files
        // have) is excluded. sql asks 4, ef-core asks 6.
        var db = await SeededDatabaseAsync();
        var sqlIds = RealTopic("sql").Questions.Select(question => question.Id).ToList();
        var efIds = RealTopic("ef-core").Questions.Select(question => question.Id).ToList();
        var sqlExcluded = sqlIds.Skip(4).ToList();
        var efExcluded = efIds.Skip(4).ToList();
        Assert.True(efExcluded.Count >= 2, $"ef-core has {efIds.Count} questions; the test needs at least 6.");
        var request = Request(AllLevels, ("sql", 4), ("ef-core", 6));
        request["excludeQuestionIds"] = new JsonArray(sqlExcluded.Concat(efExcluded).Select(id => (JsonNode)id).ToArray());

        foreach (var seed in new[] { 1, 2, 3 })
        {
            await using var factory = Factory(db, seed);
            using var client = factory.CreateClient();

            var (status, body) = await PostAsync(client, request);

            Assert.Equal(HttpStatusCode.OK, status);
            var ids = Questions(body).Select(question => Str(question, "id")).ToList();
            var sql = ids.Where(id => id.StartsWith("sql-", StringComparison.Ordinal)).ToList();
            var ef = ids.Where(id => id.StartsWith("ef-core-", StringComparison.Ordinal)).ToList();
            Assert.Equal(sqlIds.Take(4).Order(), sql.Order());
            Assert.Equal(6, ef.Count);
            Assert.Subset(ef.ToHashSet(), efIds.Take(4).ToHashSet());
            Assert.Equal(2, ef.Count(efExcluded.Contains));
        }
    }

    [Fact]
    public async Task Inactive_questions_never_get_into_a_test_and_are_not_available()
    {
        // The import cannot deactivate a question that is still in the files, so SQL does it on a throwaway database.
        // Counts come from the files, since content waves keep deepening topics (ADR 004).
        var db = await SeededDatabaseAsync();
        var (csharp, efCore, sql) = (RealTopic("csharp").Questions, RealTopic("ef-core").Questions, RealTopic("sql").Questions);
        Assert.Contains(efCore, question => question.Id == "ef-core-001");
        Assert.Contains(sql, question => question.Id == "sql-012");
        await PostgresFixture.ExecuteAsync(db, """UPDATE "Questions" SET "IsActive" = false WHERE "Id" IN ('ef-core-001', 'sql-012')""");
        await using var factory = Factory(db, seed: 3);
        using var client = factory.CreateClient();

        var (status, body) = await PostAsync(client, Request(AllLevels, ("csharp", csharp.Count), ("ef-core", efCore.Count - 1), ("sql", sql.Count - 1)));
        var (tooManyStatus, tooMany) = await PostAsync(client, Request(AllLevels, ("csharp", csharp.Count), ("ef-core", efCore.Count)));

        Assert.Equal(HttpStatusCode.OK, status);
        var ids = Questions(body).Select(question => Str(question, "id")).ToList();
        Assert.Equal(csharp.Count + efCore.Count + sql.Count - 2, ids.Count);
        Assert.DoesNotContain("ef-core-001", ids);
        Assert.DoesNotContain("sql-012", ids);
        Assert.Equal(HttpStatusCode.BadRequest, tooManyStatus);
        Assert.Equal("Test.NotEnoughQuestions", Str(tooMany, "code"));
        Assert.Contains($"доступно {efCore.Count - 1}", Str(tooMany, "message"));
    }

    [Fact]
    public async Task Invalid_requests_get_400_with_their_own_code_in_the_error_shape_of_spec_5()
    {
        var db = await SeededDatabaseAsync();
        await using var factory = Factory(db, seed: 1);
        using var client = factory.CreateClient();
        var tooManyExcluded = Request(AllLevels, ("sql", 5), ("ef-core", 5));
        tooManyExcluded["excludeQuestionIds"] = new JsonArray(Enumerable.Range(0, 10_001).Select(index => (JsonNode)$"q-{index}").ToArray());
        var sqlCount = RealTopic("sql").Questions.Count;
        var sqlJunior = RealTopic("sql").Questions.Count(question => question.LevelId == "junior");
        var missingCount = JsonNode.Parse("""{"fieldId":"it","levelIds":["junior"],"topics":[{"topicId":"sql"}]}""")!.AsObject();
        var cases = new (string Code, JsonObject Body)[]
        {
            ("Test.FieldNotFound", Request(AllLevels, ("sql", 10)).With("fieldId", "nope")),
            ("Test.FieldNotFound", Request(AllLevels, ("sql", 10)).Without("fieldId")),
            ("Test.LevelsRequired", Request([], ("sql", 10))),
            ("Test.LevelsRequired", Request(AllLevels, ("sql", 10)).Without("levelIds")),
            ("Test.UnknownLevel", Request(["junior", "architect"], ("sql", 10))),
            ("Test.DuplicateLevel", Request(["junior", "senior", "junior"], ("sql", 10))),
            ("Test.TopicsRequired", Request(AllLevels)),
            ("Test.UnknownTopic", Request(AllLevels, ("sql", 5), ("cobol", 5))),
            ("Test.DuplicateTopic", Request(AllLevels, ("sql", 5), ("sql", 5))),
            ("Test.CountTooSmall", Request(AllLevels, ("sql", 10), ("ef-core", 0))),
            ("Test.CountTooSmall", missingCount),
            ("Test.TotalOutOfRange", Request(AllLevels, ("sql", 5), ("ef-core", 4))),
            ("Test.TotalOutOfRange", Request(AllLevels, ("sql", 12), ("ef-core", 12), ("csharp", 12), ("azure", 65))),
            ("Test.NotEnoughQuestions", Request(AllLevels, ("sql", sqlCount + 1))),
            ("Test.NotEnoughQuestions", Request(["junior"], ("sql", sqlJunior + 1), ("ef-core", Math.Max(1, 9 - sqlJunior)))),
            ("Test.TooManyExcluded", tooManyExcluded),
        };

        foreach (var (code, request) in cases)
        {
            var (status, body) = await PostAsync(client, request);

            Assert.True(status == HttpStatusCode.BadRequest, $"{code}: {status} {body.ToJsonString()}");
            Assert.Equal(["code", "message"], Names(body));
            Assert.Equal(code, Str(body, "code"));
            Assert.False(string.IsNullOrWhiteSpace(Str(body, "message")));
        }
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{"fieldId":"it","levelIds":"middle","topics":[{"topicId":"sql","count":10}]}""")]
    [InlineData("""{"fieldId":"it","levelIds":["middle"],"topics":[{"topicId":"sql","count":"x"}]}""")]
    public async Task Body_of_the_wrong_shape_is_a_400_not_a_server_error(string json)
    {
        // None of these bodies gets past model binding or the field check, so no database is needed here.
        await using var factory = Factory(connectionString: null, seed: 1);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var response = await client.PostAsync("/api/tests", new StringContent(json, Encoding.UTF8, "application/json"), ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Total_of_10_and_10_000_excluded_ids_pass_validation()
    {
        var db = await SeededDatabaseAsync();
        await using var factory = Factory(db, seed: 1);
        using var client = factory.CreateClient();
        var withLimit = Request(AllLevels, ("sql", 4), ("ef-core", 6));
        withLimit["excludeQuestionIds"] = new JsonArray(Enumerable.Range(0, 10_000).Select(index => (JsonNode)$"q-{index}").ToArray());

        var (tenStatus, ten) = await PostAsync(client, Request(AllLevels, ("sql", 1), ("ef-core", 9)));
        var (limitStatus, _) = await PostAsync(client, withLimit);

        Assert.Equal(HttpStatusCode.OK, tenStatus);
        Assert.Equal(10, Questions(ten).Count);
        Assert.Equal(HttpStatusCode.OK, limitStatus);
    }

    [Fact]
    public async Task Thirty_first_post_within_a_minute_from_one_ip_gets_429()
    {
        var db = await SeededDatabaseAsync();
        await using var factory = Factory(db, seed: 1);
        using var client = factory.CreateClient();
        var request = Request(AllLevels, ("sql", 5), ("ef-core", 5));

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 31; i++)
        {
            statuses.Add((await PostAsync(client, request)).Status);
        }

        Assert.All(statuses.Take(30), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
    }

    [Fact]
    public async Task Forty_preflights_spend_no_permits_and_get_no_429()
    {
        var db = await SeededDatabaseAsync();
        await using var factory = Factory(db, seed: 1);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var request = Request(AllLevels, ("sql", 5), ("ef-core", 5));

        var preflights = new List<HttpResponseMessage>();
        for (var i = 0; i < 40; i++)
        {
            using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/tests");
            preflight.Headers.Add("Origin", FrontendDevOrigin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
            preflights.Add(await client.SendAsync(preflight, ct));
        }

        var posts = new List<HttpStatusCode>();
        for (var i = 0; i < 30; i++)
        {
            posts.Add((await PostAsync(client, request, FrontendDevOrigin)).Status);
        }

        var thirtyFirst = (await PostAsync(client, request, FrontendDevOrigin)).Status;

        Assert.All(preflights, response =>
        {
            Assert.True(response.IsSuccessStatusCode, response.StatusCode.ToString());
            Assert.Equal([FrontendDevOrigin], response.Headers.GetValues("Access-Control-Allow-Origin"));
        });
        Assert.All(posts, status => Assert.Equal(HttpStatusCode.OK, status));
        // The limit still holds after the preflights: they were simply not counted.
        Assert.Equal(HttpStatusCode.TooManyRequests, thirtyFirst);
        preflights.ForEach(response => response.Dispose());
    }

    [Fact]
    public async Task Limit_is_per_ip_and_other_endpoints_are_not_limited()
    {
        // An empty body fails on the field before any database call, so no database is needed here.
        await using var factory = Factory(connectionString: null, seed: 1);
        var first = IPAddress.Parse("203.0.113.1");
        var second = IPAddress.Parse("203.0.113.2");

        for (var i = 0; i < 30; i++)
        {
            Assert.Equal(StatusCodes.Status400BadRequest, await PostFromAsync(factory.Server, first));
        }

        Assert.Equal(StatusCodes.Status429TooManyRequests, await PostFromAsync(factory.Server, first));
        Assert.Equal(StatusCodes.Status400BadRequest, await PostFromAsync(factory.Server, second));
        using var client = factory.CreateClient();
        using var health = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    private async Task<string> SeededDatabaseAsync()
    {
        var db = await postgres.CreateDatabaseAsync();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ApiFactory.ConnectionStringKey] = db })
            .Build();
        var services = new ServiceCollection();
        services.AddApplication().AddInfrastructure(configuration);
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportContentCommand, ImportSummary>>();

        var result = await handler.Handle(new ImportContentCommand(RealContentDirectory()), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return db;
    }

    /// <summary>The API on the given database with a seeded Random in place of Random.Shared.</summary>
    private static WebApplicationFactory<Program> Factory(string? connectionString, int seed) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(ApiFactory.ConnectionStringKey, connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<Random>();
                services.AddSingleton(new Random(seed));
            });
        });

    private static async Task<List<(string Id, string Keys)>> OrderAsync(string db, int seed, JsonObject request)
    {
        await using var factory = Factory(db, seed);
        using var client = factory.CreateClient();
        var (status, body) = await PostAsync(client, request);
        Assert.Equal(HttpStatusCode.OK, status);
        return Questions(body).Select(question => (Str(question, "id"), string.Join(",", Keys(question)))).ToList();
    }

    private static JsonObject Request(IReadOnlyList<string> levels, params (string TopicId, int Count)[] topics) =>
        new()
        {
            ["fieldId"] = "it",
            ["levelIds"] = new JsonArray(levels.Select(level => (JsonNode)level).ToArray()),
            ["topics"] = new JsonArray(topics
                .Select(topic => (JsonNode)new JsonObject { ["topicId"] = topic.TopicId, ["count"] = topic.Count })
                .ToArray()),
        };

    private static async Task<(HttpStatusCode Status, JsonObject Body)> PostAsync(HttpClient client, JsonObject request, string? origin = null)
    {
        var ct = TestContext.Current.CancellationToken;
        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/tests")
        {
            Content = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        if (origin is not null)
        {
            message.Headers.Add("Origin", origin);
        }

        using var response = await client.SendAsync(message, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        return (response.StatusCode, text.Length == 0 ? new JsonObject() : JsonNode.Parse(text)!.AsObject());
    }

    private static async Task<int> PostFromAsync(TestServer server, IPAddress address)
    {
        var context = await server.SendAsync(http =>
        {
            http.Request.Method = HttpMethods.Post;
            http.Request.Path = "/api/tests";
            http.Request.ContentType = "application/json";
            http.Request.Body = new MemoryStream("{}"u8.ToArray());
            http.Connection.RemoteIpAddress = address;
        }, TestContext.Current.CancellationToken);
        return context.Response.StatusCode;
    }

    private static void AssertCode(CodeBlock? fromFile, JsonNode? code)
    {
        if (fromFile is null)
        {
            Assert.Null(code);
            return;
        }

        Assert.Equal(["lang", "source"], Names(code!.AsObject()));
        Assert.Equal((fromFile.Lang, fromFile.Source), (Str(code.AsObject(), "lang"), Str(code.AsObject(), "source")));
    }

    private static List<JsonObject> Questions(JsonObject body) =>
        body["questions"]!.AsArray().Select(question => question!.AsObject()).ToList();

    private static List<int> Keys(JsonObject question) =>
        question["options"]!.AsArray().Select(option => option!["key"]!.GetValue<int>()).ToList();

    private static string[] Names(JsonObject node) => node.Select(pair => pair.Key).ToArray();

    private static string Str(JsonObject node, string name) => node[name]!.GetValue<string>();
}

internal static class JsonObjectEdits
{
    public static JsonObject With(this JsonObject node, string name, string value)
    {
        node[name] = value;
        return node;
    }

    public static JsonObject Without(this JsonObject node, string name)
    {
        node.Remove(name);
        return node;
    }
}

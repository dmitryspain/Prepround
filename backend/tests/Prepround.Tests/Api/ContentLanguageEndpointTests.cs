using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content.Import;
using Prepround.Infrastructure;
using Prepround.Tests.Infrastructure;

namespace Prepround.Tests.Api;

/// <summary>
/// <c>?lang=</c> of <c>GET /api/catalog</c> and <c>POST /api/tests</c> (PRP-028, ADR 006, spec 5) through the real API
/// on a Testcontainers Postgres filled by the real import of a small translated content tree:
/// topic alpha (A) has 12 questions, 10 of them translated, and <c>title_en</c> / <c>description_en</c>;
/// topic beta (B) has 12 questions and no translation at all; topic gamma has 12 translated questions and only
/// <c>title_en</c>, so every title falls back to the base language on its own.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ContentLanguageEndpointTests(PostgresFixture postgres)
{
    private const string FrontendDevOrigin = "http://localhost:4300";
    private static readonly string[] AllLevels = ["junior", "middle", "senior"];

    [Fact]
    public async Task Catalog_without_lang_with_uk_and_with_empty_lang_is_the_same_bytes_in_the_base_language()
    {
        var db = await TranslatedDatabaseAsync();
        await using var factory = Factory(db);
        using var client = factory.CreateClient();

        var none = await GetTextAsync(client, "/api/catalog");
        var uk = await GetTextAsync(client, "/api/catalog?lang=uk");
        var empty = await GetTextAsync(client, "/api/catalog?lang=");

        Assert.Equal(none, uk);
        Assert.Equal(none, empty);
        var field = Field(JsonNode.Parse(none)!.AsObject());
        Assert.Equal("IT", Str(field, "title"));
        Assert.Equal(["Джуніор", "Мідл", "Сеніор"], Titles(field["levels"]!));
        // Base titles sorted: Альфа-тема (gamma), Бета-тема (beta), Ялинка (alpha).
        Assert.Equal(["gamma", "beta", "alpha"], Ids(field["topics"]!));
        var alpha = Topic(field, "alpha");
        Assert.Equal(("Ялинка", "Опис теми A."), (Str(alpha, "title"), Str(alpha, "description")));
        Assert.Equal("""{"junior":4,"middle":4,"senior":4}""", alpha["questionCounts"]!.ToJsonString());
        Assert.Equal("""{"junior":4,"middle":4,"senior":4}""", Topic(field, "beta")["questionCounts"]!.ToJsonString());
    }

    [Fact]
    public async Task English_catalog_has_translated_titles_each_falling_back_alone_counts_of_translated_questions_and_english_order()
    {
        var db = await TranslatedDatabaseAsync();
        await using var factory = Factory(db);
        using var client = factory.CreateClient();

        var english = Field(JsonNode.Parse(await GetTextAsync(client, "/api/catalog?lang=en"))!.AsObject());
        var baseline = Field(JsonNode.Parse(await GetTextAsync(client, "/api/catalog"))!.AsObject());

        Assert.Equal("IT (en)", Str(english, "title"));
        // middle has no title_en: only it falls back.
        Assert.Equal(["Junior", "Мідл", "Senior"], Titles(english["levels"]!));
        Assert.Equal(
            baseline["levels"]!.AsArray().Select(level => (Str(level!.AsObject(), "id"), level["rank"]!.GetValue<int>())),
            english["levels"]!.AsArray().Select(level => (Str(level!.AsObject(), "id"), level["rank"]!.GetValue<int>())));
        var direction = Assert.Single(english["directions"]!.AsArray())!.AsObject();
        Assert.Equal("Backend", Str(direction, "title"));
        Assert.Equal([".NET (C#) en", "Інший стек"], Titles(direction["stacks"]!));
        Assert.Equal(
            baseline["directions"]![0]!["stacks"]!.ToJsonString().Replace(".NET (C#)\"", ".NET (C#) en\"", StringComparison.Ordinal),
            direction["stacks"]!.ToJsonString());

        // Shown titles sorted: "Alpha in English" (alpha), "Zulu in English" (gamma), "Бета-тема" (beta).
        // The base order (gamma, beta, alpha) and the id order (alpha, beta, gamma) both differ from it.
        Assert.Equal(["alpha", "gamma", "beta"], Ids(english["topics"]!));

        var a = Topic(english, "alpha");
        Assert.Equal(("Alpha in English", "Description of topic A."), (Str(a, "title"), Str(a, "description")));
        Assert.Equal("""{"junior":4,"middle":4,"senior":2}""", a["questionCounts"]!.ToJsonString());
        Assert.Equal(10, a["questionCounts"]!.AsObject().Sum(pair => pair.Value!.GetValue<int>()));

        var b = Topic(english, "beta");
        Assert.Equal(("Бета-тема", "Опис теми B."), (Str(b, "title"), Str(b, "description")));
        Assert.Equal("{}", b["questionCounts"]!.ToJsonString());

        // A translated title with an untranslated description: each field falls back on its own.
        var gamma = Topic(english, "gamma");
        Assert.Equal(("Zulu in English", "Опис теми C."), (Str(gamma, "title"), Str(gamma, "description")));
        Assert.Equal("""{"junior":4,"middle":4,"senior":4}""", gamma["questionCounts"]!.ToJsonString());
    }

    [Theory]
    [InlineData("de", "de")]
    [InlineData("EN", "EN")]
    [InlineData("%20en", " en")]
    [InlineData("en%20", "en ")]
    [InlineData("uk-UA", "uk-UA")]
    public async Task Unsupported_language_is_400_Language_Unsupported_on_both_endpoints_before_any_rule_of_the_body(
        string sent, string shown)
    {
        // The language is checked before anything else, so no database is needed to get this error.
        await using var factory = Factory(connectionString: null);
        using var client = factory.CreateClient();
        var expected = JsonNode.Parse(
            $$"""{"code":"Language.Unsupported","message":"Мову «{{shown}}» не підтримано. Доступні: uk, en."}""")!.ToJsonString();

        using var catalog = await client.GetAsync($"/api/catalog?lang={sent}", TestContext.Current.CancellationToken);
        var emptyBody = await PostAsync(client, $"/api/tests?lang={sent}", new JsonObject());
        var validBody = await PostAsync(client, $"/api/tests?lang={sent}", Request(("alpha", 10)));

        Assert.Equal(HttpStatusCode.BadRequest, catalog.StatusCode);
        Assert.Equal(expected, JsonNode.Parse(await catalog.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!.ToJsonString());
        Assert.Equal((HttpStatusCode.BadRequest, expected), (emptyBody.Status, emptyBody.Body.ToJsonString()));
        Assert.Equal((HttpStatusCode.BadRequest, expected), (validBody.Status, validBody.Body.ToJsonString()));
    }

    [Fact]
    public async Task English_test_has_only_translated_questions_with_english_texts_and_the_keys_and_answers_of_the_base()
    {
        var db = await TranslatedDatabaseAsync();
        await using var factory = Factory(db);
        using var client = factory.CreateClient();

        var english = await PostAsync(client, "/api/tests?lang=en", Request(("alpha", 10)));
        var ukrainian = await PostAsync(client, "/api/tests?lang=uk", Request(("alpha", 12)));

        Assert.Equal(HttpStatusCode.OK, english.Status);
        Assert.Equal(HttpStatusCode.OK, ukrainian.Status);
        var englishQuestions = Questions(english.Body);
        var ukrainianQuestions = Questions(ukrainian.Body).ToDictionary(question => Str(question, "id"));
        Assert.Equal(Enumerable.Range(1, 10).Select(QuestionId), englishQuestions.Select(question => Str(question, "id")).Order());
        Assert.Equal(Enumerable.Range(1, 12).Select(QuestionId), ukrainianQuestions.Keys.Order());

        Assert.All(englishQuestions, question =>
        {
            var id = Str(question, "id");
            var inBase = ukrainianQuestions[id];
            Assert.Equal(["id", "topicId", "levelId", "type", "text", "code", "options", "explanation"], question.Select(pair => pair.Key));
            Assert.Equal($"Question {id}?", Str(question, "text"));
            Assert.Equal($"Explanation of {id}.", Str(question, "explanation"));
            Assert.All(question["options"]!.AsArray(), option =>
                Assert.Equal($"Option {id}-{option!["key"]!.GetValue<int>()}", Str(option.AsObject(), "text")));
            // Everything but the texts is that of the base row.
            foreach (var name in new[] { "topicId", "levelId", "type", "code" })
            {
                Assert.Equal(inBase[name]?.ToJsonString(), question[name]?.ToJsonString());
            }

            Assert.Equal(Answers(inBase), Answers(question));
            Assert.Equal($"Питання {id}?", Str(inBase, "text"));
            Assert.Equal($"Пояснення до {id}.", Str(inBase, "explanation"));
            Assert.All(inBase["options"]!.AsArray(), option =>
                Assert.Equal($"Варіант {id}-{option!["key"]!.GetValue<int>()}", Str(option.AsObject(), "text")));
        });
        // The fixture really has code blocks and both question types among the translated questions.
        Assert.Contains(englishQuestions, question => question["code"] is not null);
        Assert.Contains(englishQuestions, question => Str(question, "type") == "multiple");
    }

    [Fact]
    public async Task English_limit_counts_only_translated_questions_and_uk_takes_all_of_them()
    {
        var db = await TranslatedDatabaseAsync();
        await using var factory = Factory(db);
        using var client = factory.CreateClient();

        var elevenInEnglish = await PostAsync(client, "/api/tests?lang=en", Request(("alpha", 11)));
        var untranslatedTopic = await PostAsync(client, "/api/tests?lang=en", Request(("beta", 10)));
        var twelveInUkrainian = await PostAsync(client, "/api/tests?lang=uk", Request(("alpha", 12)));
        // Only the query string sets the language: a "language" in the body is not read.
        var languageInBody = await PostAsync(client, "/api/tests", Request(("beta", 10)).With("language", "en").With("lang", "en"));

        Assert.Equal((HttpStatusCode.BadRequest, "Test.NotEnoughQuestions"), (elevenInEnglish.Status, Str(elevenInEnglish.Body, "code")));
        Assert.Equal((HttpStatusCode.BadRequest, "Test.NotEnoughQuestions"), (untranslatedTopic.Status, Str(untranslatedTopic.Body, "code")));
        Assert.Equal(HttpStatusCode.OK, twelveInUkrainian.Status);
        Assert.Equal(12, Questions(twelveInUkrainian.Body).Count);
        Assert.Equal(HttpStatusCode.OK, languageInBody.Status);
        Assert.All(Questions(languageInBody.Body), question => Assert.StartsWith("Питання ", Str(question, "text")));
    }

    [Fact]
    public async Task Deactivated_translated_question_is_neither_counted_nor_picked_in_English()
    {
        var db = await TranslatedDatabaseAsync();
        await PostgresFixture.ExecuteAsync(db, """UPDATE "Questions" SET "IsActive" = false WHERE "Id" = 'alpha-001'""");
        await using var factory = Factory(db);
        using var client = factory.CreateClient();

        var catalog = Field(JsonNode.Parse(await GetTextAsync(client, "/api/catalog?lang=en"))!.AsObject());
        var ten = await PostAsync(client, "/api/tests?lang=en", Request(("alpha", 10)));
        // The total must stay within 10–100, so one gamma question fills it up.
        var nine = await PostAsync(client, "/api/tests?lang=en", Request(("alpha", 9), ("gamma", 1)));

        Assert.Equal("""{"junior":3,"middle":4,"senior":2}""", Topic(catalog, "alpha")["questionCounts"]!.ToJsonString());
        Assert.Equal((HttpStatusCode.BadRequest, "Test.NotEnoughQuestions"), (ten.Status, Str(ten.Body, "code")));
        Assert.Equal(HttpStatusCode.OK, nine.Status);
        Assert.Equal(
            Enumerable.Range(2, 9).Select(QuestionId),
            Questions(nine.Body).Select(question => Str(question, "id")).Where(id => id.StartsWith("alpha-", StringComparison.Ordinal)).Order());
    }

    [Fact]
    public async Task English_test_costs_the_same_queries_for_10_and_20_questions_and_one_more_than_the_base_test()
    {
        var db = await TranslatedDatabaseAsync();
        var commands = new CommandLog();
        await using var factory = Factory(db, commands);
        using var client = factory.CreateClient();

        var tenInEnglish = await CountedPostAsync(client, commands, "/api/tests?lang=en", Request(("alpha", 10)));
        var twentyInEnglish = await CountedPostAsync(client, commands, "/api/tests?lang=en", Request(("alpha", 10), ("gamma", 10)));
        var twentyInBase = await CountedPostAsync(client, commands, "/api/tests", Request(("alpha", 10), ("beta", 10)));

        Assert.Equal(tenInEnglish.Count, twentyInEnglish.Count);
        Assert.Equal(twentyInBase.Count + 1, twentyInEnglish.Count);
        Assert.True(twentyInEnglish.Count <= 6, string.Join(Environment.NewLine + "---" + Environment.NewLine, twentyInEnglish));
        // The base test never touches translations, so its queries are those it had before PRP-028.
        Assert.DoesNotContain(twentyInBase, command => command.Contains("QuestionTranslations", StringComparison.Ordinal));
        Assert.Single(twentyInEnglish, command =>
            command.Contains("FROM \"QuestionTranslations\"", StringComparison.Ordinal) && !command.Contains("EXISTS", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Preflight_cors_headers_and_the_rate_limit_do_not_depend_on_the_language()
    {
        await using var factory = Factory(connectionString: null);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/tests?lang=de");
        preflight.Headers.Add("Origin", FrontendDevOrigin);
        preflight.Headers.Add("Access-Control-Request-Method", "POST");
        preflight.Headers.Add("Access-Control-Request-Headers", "content-type");
        using var preflightResponse = await client.SendAsync(preflight, ct);
        using var catalog = new HttpRequestMessage(HttpMethod.Get, "/api/catalog?lang=de");
        catalog.Headers.Add("Origin", FrontendDevOrigin);
        using var catalogResponse = await client.SendAsync(catalog, ct);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 31; i++)
        {
            var language = i % 2 == 0 ? "de" : "en";
            statuses.Add((await PostAsync(client, $"/api/tests?lang={language}", new JsonObject(), FrontendDevOrigin)).Status);
        }

        Assert.True(preflightResponse.IsSuccessStatusCode, preflightResponse.StatusCode.ToString());
        Assert.Equal([FrontendDevOrigin], preflightResponse.Headers.GetValues("Access-Control-Allow-Origin"));
        // The frontend reads the code of a 400 only if CORS lets it.
        Assert.Equal(HttpStatusCode.BadRequest, catalogResponse.StatusCode);
        Assert.Equal([FrontendDevOrigin], catalogResponse.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.All(statuses.Take(30), status => Assert.Equal(HttpStatusCode.BadRequest, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[30]);
    }

    private static string QuestionId(int number) => $"alpha-{number:000}";

    /// <summary>Correct keys of a question, in key order: the same set means the same answer.</summary>
    private static string Answers(JsonObject question) =>
        string.Join(",", question["options"]!.AsArray()
            .Select(option => (Key: option!["key"]!.GetValue<int>(), Correct: option["isCorrect"]!.GetValue<bool>()))
            .OrderBy(option => option.Key)
            .Select(option => $"{option.Key}:{option.Correct}"));

    private async Task<string> TranslatedDatabaseAsync()
    {
        var db = await postgres.CreateDatabaseAsync();
        var root = Directory.CreateTempSubdirectory("prepround-lang-content-").FullName;
        try
        {
            TranslatedContent.WriteTo(root);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ApiFactory.ConnectionStringKey] = db })
                .Build();
            var services = new ServiceCollection();
            services.AddApplication().AddInfrastructure(configuration);
            await using var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportContentCommand, ImportSummary>>();

            var result = await handler.Handle(new ImportContentCommand(root), TestContext.Current.CancellationToken);

            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }

        return db;
    }

    /// <summary>The API on the given database with a seeded Random, and optionally a log of every SQL command it sends.</summary>
    private static WebApplicationFactory<Program> Factory(string? connectionString, CommandLog? commands = null) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting(ApiFactory.ConnectionStringKey, connectionString);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<Random>();
                services.AddSingleton(new Random(1));
                if (commands is not null)
                {
                    services.ConfigureDbContext<PreproundContext>(options => options.AddInterceptors(commands));
                }
            });
        });

    private static JsonObject Request(params (string TopicId, int Count)[] topics) =>
        new()
        {
            ["fieldId"] = "it",
            ["levelIds"] = new JsonArray(AllLevels.Select(level => (JsonNode)level).ToArray()),
            ["topics"] = new JsonArray(topics
                .Select(topic => (JsonNode)new JsonObject { ["topicId"] = topic.TopicId, ["count"] = topic.Count })
                .ToArray()),
        };

    private static async Task<string> GetTextAsync(HttpClient client, string url)
    {
        var ct = TestContext.Current.CancellationToken;
        using var response = await client.GetAsync(url, ct);
        var text = await response.Content.ReadAsStringAsync(ct);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{url}: {(int)response.StatusCode} {text}");
        return text;
    }

    private static async Task<(HttpStatusCode Status, JsonObject Body)> PostAsync(
        HttpClient client, string url, JsonObject request, string? origin = null)
    {
        var ct = TestContext.Current.CancellationToken;
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
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

    private static async Task<IReadOnlyList<string>> CountedPostAsync(HttpClient client, CommandLog commands, string url, JsonObject request)
    {
        commands.Clear();
        var (status, body) = await PostAsync(client, url, request);
        Assert.True(status == HttpStatusCode.OK, body.ToJsonString());
        return commands.Snapshot();
    }

    private static JsonObject Field(JsonObject body) => Assert.Single(body["fields"]!.AsArray())!.AsObject();

    private static JsonObject Topic(JsonObject field, string id) =>
        Assert.Single(field["topics"]!.AsArray(), topic => Str(topic!.AsObject(), "id") == id)!.AsObject();

    private static List<string> Ids(JsonNode array) => array.AsArray().Select(node => Str(node!.AsObject(), "id")).ToList();

    private static List<string> Titles(JsonNode array) => array.AsArray().Select(node => Str(node!.AsObject(), "title")).ToList();

    private static List<JsonObject> Questions(JsonObject body) =>
        body["questions"]!.AsArray().Select(question => question!.AsObject()).ToList();

    private static string Str(JsonObject node, string name) => node[name]!.GetValue<string>();

    /// <summary>Every command text the API sends to the database, so a test can count round trips.</summary>
    private sealed class CommandLog : DbCommandInterceptor
    {
        private readonly ConcurrentQueue<string> _commands = new();

        public void Clear() => _commands.Clear();

        public IReadOnlyList<string> Snapshot() => _commands.ToList();

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            _commands.Enqueue(command.CommandText);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            _commands.Enqueue(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}

/// <summary>
/// The content tree of <see cref="ContentLanguageEndpointTests"/>, written as YAML. Every question has 4 options;
/// the correct one moves with the number, every fourth question is multiple and every third has code, so keys,
/// answers, types and code are all really compared between the languages.
/// </summary>
internal static class TranslatedContent
{
    public static void WriteTo(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "it", "topics"));
        File.WriteAllText(Path.Combine(root, "it", "field.yaml"), """
            id: it
            title: IT
            title_en: IT (en)
            levels:
              - id: junior
                title: Джуніор
                title_en: Junior
              - id: middle
                title: Мідл
              - id: senior
                title: Сеніор
                title_en: Senior
            directions:
              - id: backend
                title: Бекенд
                title_en: Backend
                stacks:
                  - id: dotnet
                    title: .NET (C#)
                    title_en: .NET (C#) en
                    topics: [alpha, beta, gamma]
                  - id: other
                    title: Інший стек
                    topics: [beta]

            """);
        File.WriteAllText(Path.Combine(root, "it", "topics", "alpha.yaml"),
            Topic("alpha", "Ялинка", "Alpha in English", "Опис теми A.", "Description of topic A.", translated: 10));
        File.WriteAllText(Path.Combine(root, "it", "topics", "beta.yaml"),
            Topic("beta", "Бета-тема", null, "Опис теми B.", null, translated: 0));
        File.WriteAllText(Path.Combine(root, "it", "topics", "gamma.yaml"),
            Topic("gamma", "Альфа-тема", "Zulu in English", "Опис теми C.", null, translated: 12));
    }

    /// <summary>12 questions, 4 per level in scale order; the first <paramref name="translated"/> get a full translation.</summary>
    private static string Topic(string id, string title, string? titleEn, string description, string? descriptionEn, int translated)
    {
        var yaml = new StringBuilder();
        yaml.Append($"id: {id}\ntitle: {title}\n");
        if (titleEn is not null)
        {
            yaml.Append($"title_en: {titleEn}\n");
        }

        yaml.Append($"description: {description}\n");
        if (descriptionEn is not null)
        {
            yaml.Append($"description_en: {descriptionEn}\n");
        }

        yaml.Append("questions:\n");
        string[] levels = ["junior", "middle", "senior"];
        for (var number = 1; number <= 12; number++)
        {
            var questionId = $"{id}-{number:000}";
            var english = number <= translated;
            var multiple = number % 4 == 0;
            int[] correct = multiple ? [number % 4, (number + 1) % 4] : [number % 4];

            yaml.Append($"  - id: {questionId}\n");
            yaml.Append($"    level: {levels[(number - 1) / 4]}\n");
            yaml.Append($"    type: {(multiple ? "multiple" : "single")}\n");
            yaml.Append($"    text: Питання {questionId}?\n");
            if (english)
            {
                yaml.Append($"    text_en: Question {questionId}?\n");
            }

            if (number % 3 == 0)
            {
                yaml.Append($"    code:\n      lang: csharp\n      source: |\n        var answer = {number};\n");
            }

            yaml.Append("    options:\n");
            for (var key = 0; key < 4; key++)
            {
                yaml.Append($"      - text: Варіант {questionId}-{key}\n");
                if (english)
                {
                    yaml.Append($"        text_en: Option {questionId}-{key}\n");
                }

                if (correct.Contains(key))
                {
                    yaml.Append("        correct: true\n");
                }
            }

            yaml.Append($"    explanation: Пояснення до {questionId}.\n");
            if (english)
            {
                yaml.Append($"    explanation_en: Explanation of {questionId}.\n");
            }
        }

        return yaml.ToString();
    }
}

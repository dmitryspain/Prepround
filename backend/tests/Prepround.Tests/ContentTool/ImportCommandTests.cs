using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using Prepround.Domain.Catalog;
using Prepround.Infrastructure.Content;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// <c>import &lt;dir&gt;</c> through the real process against a Testcontainers Postgres (PRP-004, ADR 002).
/// The connection string always comes from the environment and HOME is private, so the owner's user-secrets
/// never take part.
/// </summary>
[Collection(PostgresCollection.Name)]
public class ImportCommandTests(PostgresFixture postgres)
{
    private const string FieldFile = "it/field.yaml";
    private const string SqlFile = "it/topics/sql.yaml";
    private const string Done = "Імпорт завершено.";

    private const string ArchitectLevel = """
          - id: senior
            title: Senior
          - id: architect
            title: Architect
        """;

    /// <summary>A sql question on the new level; its id is the first free one, since sql keeps growing (ADR 004).</summary>
    private static string ArchitectQuestionId => FreeQuestionId("sql");

    private static string ArchitectQuestion => $"""

          - id: {ArchitectQuestionId}
            level: architect
            type: single
            text: Питання для рівня, якого в коді немає?
            options:
              - text: Так
                correct: true
              - text: Ні
            explanation: Рівні — це дані з field.yaml.
        """;

    private static readonly string[] NothingChanged = Summary(
        fields: (0, 0, 0), levels: (0, 0, 0), directions: (0, 0, 0), stacks: (0, 0, 0), topics: (0, 0, 0), questions: (0, 0, 0));

    /// <summary>One way to break the copy per case of the criterion: a reader error and a rule 6 error.</summary>
    private static readonly Dictionary<string, Action<RealContentCopy>> Breakages = new()
    {
        ["yaml-syntax-error"] = c => c.Replace(SqlFile, "text: Чим `WHERE`", "text: [Чим `WHERE`"),
        ["rule-6-unknown-type"] = c => c.Replace(SqlFile, "type: single", "type: singel"),
    };

    [Fact]
    public async Task Real_content_is_imported_whole_and_a_repeat_changes_nothing()
    {
        var db = await postgres.CreateDatabaseAsync();
        var content = RealContentDirectory();
        // Guards the raw-line count itself: seed 36 + PRP-018 36. Content only grows, so this is a lower bound.
        Assert.True(RealQuestionCount() >= 72, $"RealQuestionCount() = {RealQuestionCount()}");

        var first = await RunIsolatedAsync(db, null, "import", content);

        Assert.True(first.ExitCode == SuccessExitCode, first.Output);
        Assert.Empty(first.Stderr);
        Assert.Equal(
            Summary(fields: (1, 0, 0), levels: (3, 0, 0), directions: (2, 0, 0), stacks: (9, 0, 0), topics: (61, 0, 0), questions: (RealQuestionCount(), 0, 0)),
            first.StdoutLines);
        var afterFirst = await PostgresFixture.SnapshotAsync(db);

        var second = await RunIsolatedAsync(db, null, "import", content);

        Assert.True(second.ExitCode == SuccessExitCode, second.Output);
        Assert.Equal(NothingChanged, second.StdoutLines);
        Assert.Equal(afterFirst, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Imported_rows_are_active_and_UpdatedAt_is_utc_time_of_the_import()
    {
        var db = await postgres.CreateDatabaseAsync();
        var before = DateTimeOffset.UtcNow;

        var run = await RunIsolatedAsync(db, null, "import", RealContentDirectory());

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        await using var context = PostgresFixture.CreateContext(db);
        var questions = await context.Questions.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(RealQuestionCount(), questions.Count);
        Assert.All(questions, question =>
        {
            Assert.True(question.IsActive);
            Assert.Equal(TimeSpan.Zero, question.UpdatedAt.Offset);
            Assert.InRange(question.UpdatedAt, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
        });
        Assert.True(await context.StackTopics.AllAsync(link => link.IsActive, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Question_removed_from_the_file_is_deactivated_not_deleted_and_reactivated_when_back()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        var original = content.Read(SqlFile);
        // The last question of the file, whatever its number: sql keeps growing (ADR 004).
        var last = RealTopic("sql").Questions[^1].Id;
        await ImportOkAsync(db, content.Root);
        var lastBefore = await QuestionAsync(db, last);

        content.Write(SqlFile, original[..original.IndexOf($"  - id: {last}\n", StringComparison.Ordinal)]);
        var removed = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(removed.ExitCode == SuccessExitCode, removed.Output);
        Assert.Equal(Summary(questions: (0, 0, 1)), removed.StdoutLines);
        var deactivated = await QuestionAsync(db, last);
        Assert.False(deactivated.IsActive);
        Assert.Equal(lastBefore.UpdatedAt, deactivated.UpdatedAt);
        Assert.Equal(RealQuestionCount(), await CountAsync(db, context => context.Questions));

        content.Write(SqlFile, original);
        var returned = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(returned.ExitCode == SuccessExitCode, returned.Output);
        Assert.Equal(Summary(questions: (0, 1, 0)), returned.StdoutLines);
        var reactivated = await QuestionAsync(db, last);
        Assert.True(reactivated.IsActive);
        // Only the activity changed, not the content.
        Assert.Equal(lastBefore.UpdatedAt, reactivated.UpdatedAt);
    }

    [Fact]
    public async Task Changing_only_an_explanation_updates_that_question_and_only_its_UpdatedAt()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);
        var before = await UpdatedAtByIdAsync(db);

        content.Replace(SqlFile, "`WHERE` відкидає рядки ще до", "`WHERE` відсікає рядки ще до");
        var run = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(Summary(questions: (0, 1, 0)), run.StdoutLines);
        var after = await UpdatedAtByIdAsync(db);
        var changed = Assert.Single(after, pair => pair.Value != before[pair.Key]);
        Assert.Equal("sql-001", changed.Key);
        Assert.True(changed.Value > before["sql-001"]);
        Assert.Contains("відсікає", (await QuestionAsync(db, "sql-001")).Explanation);
    }

    [Fact]
    public async Task New_level_in_field_yaml_is_imported_with_the_next_rank_without_code_changes()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);

        content.Replace(FieldFile, "  - id: senior\n    title: Senior", ArchitectLevel);
        content.Write(SqlFile, content.Read(SqlFile) + ArchitectQuestion);
        var run = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(Summary(levels: (1, 0, 0), questions: (1, 0, 0)), run.StdoutLines);
        await using var context = PostgresFixture.CreateContext(db);
        var levels = await context.Levels.AsNoTracking().OrderBy(level => level.Rank).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            [("junior", 1), ("middle", 2), ("senior", 3), ("architect", 4)],
            levels.Select(level => (level.Id, level.Rank)));
        Assert.All(levels, level => Assert.True(level.IsActive));
        var question = await QuestionAsync(db, ArchitectQuestionId);
        Assert.Equal(("it", "sql", "architect", true), (question.FieldId, question.TopicId, question.LevelId, question.IsActive));
    }

    [Fact]
    public async Task Sources_live_only_in_yaml_so_adding_them_changes_nothing_in_the_database()
    {
        // ADR 004, section 3: no column, no migration; the question row is the same with and without sources.
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);
        var before = await PostgresFixture.SnapshotAsync(db);

        content.Replace(SqlFile, "  - id: sql-001\n    level: junior\n", """
              - id: sql-001
                level: junior
                sources:
                  - https://www.postgresql.org/docs/18/queries-table-expressions.html#QUERIES-GROUP

            """);
        var run = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(NothingChanged, run.StdoutLines);
        Assert.Equal(before, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Stack_topics_options_and_scale_keep_the_order_of_the_files()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportOkAsync(db, RealContentDirectory());
        var ct = TestContext.Current.CancellationToken;
        var field = Assert.Single(new YamlContentReader().Read(RealContentDirectory()).Fields);
        await using var context = PostgresFixture.CreateContext(db);

        var levels = await context.Levels.AsNoTracking().OrderBy(level => level.Rank).Select(level => level.Id).ToListAsync(ct);
        Assert.Equal(field.Levels.Select(level => level.Id), levels);

        var directions = await context.Directions.AsNoTracking().OrderBy(direction => direction.Order).Select(direction => direction.Id).ToListAsync(ct);
        Assert.Equal(field.Directions.Select(direction => direction.Id), directions);

        var links = await context.StackTopics.AsNoTracking().ToListAsync(ct);
        var stacks = await context.Stacks.AsNoTracking().ToListAsync(ct);
        foreach (var direction in field.Directions)
        {
            Assert.Equal(
                direction.Stacks.Select(stack => stack.Id),
                stacks.Where(stack => stack.DirectionId == direction.Id).OrderBy(stack => stack.Order).Select(stack => stack.Id));

            foreach (var stack in direction.Stacks)
            {
                Assert.Equal(
                    stack.TopicIds,
                    links.Where(link => link.StackId == stack.Id).OrderBy(link => link.Order).Select(link => link.TopicId));
            }
        }

        var questions = (await context.Questions.AsNoTracking().ToListAsync(ct)).ToDictionary(question => question.Id);
        foreach (var definition in field.Topics.SelectMany(topic => topic.Questions))
        {
            Assert.Equal(
                definition.Options.Select(option => new QuestionOption(option.Text, option.IsCorrect)),
                questions[definition.Id].Options);
        }
    }

    [Fact]
    public async Task Options_are_stored_as_a_jsonb_array_of_text_and_isCorrect()
    {
        var db = await postgres.CreateDatabaseAsync();
        await ImportOkAsync(db, RealContentDirectory());
        await using var context = PostgresFixture.CreateContext(db);

        var json = await context.Database
            .SqlQueryRaw<string>("""SELECT "Options"::text AS "Value" FROM "Questions" WHERE "Id" = 'sql-002'""")
            .SingleAsync(TestContext.Current.CancellationToken);
        var type = await context.Database
            .SqlQueryRaw<string>("""SELECT jsonb_typeof("Options") AS "Value" FROM "Questions" WHERE "Id" = 'sql-002'""")
            .SingleAsync(TestContext.Current.CancellationToken);

        Assert.Equal("array", type);
        var options = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
        Assert.Equal(["`4`", "`3`", "`5`", "`9`"], options.Select(option => option.GetProperty("text").GetString()));
        Assert.Equal([true, false, false, false], options.Select(option => option.GetProperty("isCorrect").GetBoolean()));
        Assert.All(options, option =>
            Assert.Equal(["isCorrect", "text"], option.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)));
    }

    [Theory]
    [InlineData("yaml-syntax-error")]
    [InlineData("rule-6-unknown-type")]
    public async Task Broken_content_prints_the_same_lines_as_validate_exits_with_1_and_changes_nothing(string breakage)
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);
        var before = await PostgresFixture.SnapshotAsync(db);
        Breakages[breakage](content);

        var validate = await RunIsolatedAsync(null, null, "validate", content.Root);
        var import = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(import.ExitCode == ContentErrorExitCode, import.Output);
        Assert.Equal(ContentErrorExitCode, validate.ExitCode);
        Assert.Empty(import.Stdout);
        Assert.Equal(validate.Stderr, import.Stderr);
        Assert.Contains(import.StderrLines, line => line.StartsWith($"{SqlFile}: ", StringComparison.Ordinal));
        Assert.Equal("Помилок у контенті: 1.", import.StderrLines[^1]);
        Assert.Equal(before, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Topic_file_renamed_to_yml_after_import_exits_with_1_and_deactivates_nothing()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);
        var before = await PostgresFixture.SnapshotAsync(db);
        content.Move(SqlFile, "it/topics/sql.yml");

        var run = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Contains(run.StderrLines, line => line.StartsWith("it/topics/sql.yml:", StringComparison.Ordinal));
        Assert.Equal(before, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Database_failure_in_the_middle_of_the_import_rolls_back_every_change_and_exits_with_3()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = new RealContentCopy();
        await ImportOkAsync(db, content.Root);
        // The new level and the changed explanation are written before the failing question insert.
        await PostgresFixture.ExecuteAsync(db, $"""
            CREATE FUNCTION fail_import() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'test: insert of % refused', NEW."Id"; END $$;
            CREATE TRIGGER fail_import BEFORE INSERT ON "Questions"
            FOR EACH ROW WHEN (NEW."Id" = '{ArchitectQuestionId}') EXECUTE FUNCTION fail_import();
            """);
        var before = await PostgresFixture.SnapshotAsync(db);
        content.Replace(FieldFile, "  - id: senior\n    title: Senior", ArchitectLevel);
        content.Replace(SqlFile, "`WHERE` відкидає рядки ще до", "`WHERE` відсікає рядки ще до");
        content.Write(SqlFile, content.Read(SqlFile) + ArchitectQuestion);

        var run = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Contains("Помилка бази даних під час імпорту", run.Stderr);
        Assert.Contains(ArchitectQuestionId, run.Stderr);
        Assert.Equal(before, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Database_without_migrations_exits_with_3_names_them_and_suggests_ef_database_update()
    {
        var db = await postgres.CreateDatabaseAsync(migrate: false);

        var run = await RunIsolatedAsync(db, null, "import", RealContentDirectory());

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains("InitialCatalog", run.Stderr);
        Assert.Contains("dotnet ef database update", run.Stderr);
    }

    [Fact]
    public async Task Environment_variable_overrides_user_secrets_in_the_real_process()
    {
        var db = await postgres.CreateDatabaseAsync();
        var home = Directory.CreateTempSubdirectory("prepround-home-");
        try
        {
            // The fake user-secrets point at a working database: what wins is visible from the outcome.
            var secrets = Path.Combine(home.FullName, ".microsoft", "usersecrets", UserSecretsId(), "secrets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(secrets)!);
            await File.WriteAllTextAsync(secrets, JsonSerializer.Serialize(new { ConnectionStrings = new { Prepround = db } }),
                TestContext.Current.CancellationToken);

            var fromSecrets = await RunIsolatedAsync(null, home.FullName, "import", RealContentDirectory());
            var overriddenByUnreachable = await RunIsolatedAsync(ImportWithoutDatabaseTests.Unreachable, home.FullName, "import", RealContentDirectory());
            var overriddenByEmpty = await RunIsolatedAsync("", home.FullName, "import", RealContentDirectory());

            Assert.True(fromSecrets.ExitCode == SuccessExitCode, fromSecrets.Output);
            Assert.True(overriddenByUnreachable.ExitCode == DatabaseErrorExitCode, overriddenByUnreachable.Output);
            Assert.Contains("Не вдалося підключитися до бази даних", overriddenByUnreachable.Stderr);
            Assert.True(overriddenByEmpty.ExitCode == DatabaseErrorExitCode, overriddenByEmpty.Output);
            Assert.Contains("dotnet user-secrets set", overriddenByEmpty.Stderr);
        }
        finally
        {
            home.Delete(recursive: true);
        }
    }

    private static async Task ImportOkAsync(string connectionString, string root)
    {
        var run = await RunIsolatedAsync(connectionString, null, "import", root);
        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
    }

    private static async Task<Question> QuestionAsync(string connectionString, string id)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await context.Questions.AsNoTracking().SingleAsync(question => question.Id == id, TestContext.Current.CancellationToken);
    }

    private static async Task<Dictionary<string, DateTimeOffset>> UpdatedAtByIdAsync(string connectionString)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await context.Questions.AsNoTracking().ToDictionaryAsync(
            question => question.Id, question => question.UpdatedAt, TestContext.Current.CancellationToken);
    }

    private static async Task<int> CountAsync<T>(string connectionString, Func<Prepround.Infrastructure.PreproundContext, IQueryable<T>> table)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await table(context).CountAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>What a successful import prints: a header and one line per entity type.</summary>
    private static string[] Summary(
        (int, int, int) fields = default, (int, int, int) levels = default, (int, int, int) directions = default,
        (int, int, int) stacks = default, (int, int, int) topics = default, (int, int, int) questions = default) =>
    [
        Done,
        Counts("Галузі", fields),
        Counts("Рівні", levels),
        Counts("Напрями", directions),
        Counts("Стеки", stacks),
        Counts("Теми", topics),
        Counts("Питання", questions),
    ];

    private static string Counts(string entities, (int Added, int Updated, int Deactivated) counts) =>
        $"{entities}: додано {counts.Added}, оновлено {counts.Updated}, деактивовано {counts.Deactivated}";

    private static string UserSecretsId() =>
        Assembly.Load("Prepround.ContentTool").GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;
}

/// <summary>import failures that need no database at all, so these run without Docker and in parallel.</summary>
public class ImportWithoutDatabaseTests
{
    /// <summary>Nothing listens on port 1: the same refused connection as a stopped Postgres container.</summary>
    public const string Unreachable = "Host=127.0.0.1;Port=1;Database=prepround;Username=prepround;Password=prepround;Timeout=2";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Missing_connection_string_exits_with_3_and_suggests_user_secrets(string? connectionString)
    {
        var run = await RunIsolatedAsync(connectionString, null, "import", RealContentDirectory());

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains("Не задано рядок підключення", run.Stderr);
        Assert.Contains("dotnet user-secrets set \"ConnectionStrings:Prepround\"", run.Stderr);
    }

    [Fact]
    public async Task Unreachable_database_exits_with_3_and_says_so_in_ukrainian()
    {
        var run = await RunIsolatedAsync(Unreachable, null, "import", RealContentDirectory());

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains("Не вдалося підключитися до бази даних", run.Stderr);
    }

    [Fact]
    public async Task Broken_content_is_reported_before_the_database_is_needed()
    {
        using var content = new RealContentCopy();
        content.Replace("it/topics/sql.yaml", "type: single", "type: singel");

        var run = await RunIsolatedAsync(Unreachable, null, "import", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Contains(run.StderrLines, line => line.StartsWith("it/topics/sql.yaml: sql-001: ", StringComparison.Ordinal));
    }
}

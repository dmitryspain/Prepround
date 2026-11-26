using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Prepround.Domain.Catalog;
using Prepround.Tests.Infrastructure;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// PRP-026 (ADR 006, section 2): <c>import</c> stores the translations beside the base content, through the real
/// process against a Testcontainers Postgres. "Питання" never counts a translation change, so these tests pin both
/// summary lines and the base rows (UpdatedAt included) at every step.
/// </summary>
public partial class ImportCommandTests
{
    private const string SqlTitle = "title: SQL і реляційні БД\n";

    [Fact]
    public async Task One_question_translation_is_added_updated_and_removed_and_the_questions_never_change()
    {
        var db = await postgres.CreateDatabaseAsync();
        // sql holds English back while it is translated question by question (rule 12), so, as in translation
        // waves, its translations reach the database only with --include-drafts: every import here has the flag.
        using var content = WithUntranslatedSql();
        var original = content.Read(SqlFile);
        // A multiple-choice question: OptionTexts must keep the file order of all its options.
        var question = RealTopic("sql").Questions.First(definition => definition.Type == "multiple" && definition.Options.Count > 3);
        await ImportOkAsync(db, content.Root, IncludeDrafts);
        var baseRows = await PostgresFixture.SnapshotAsync(db);

        var english = TranslateQuestion(content, question.Id, "first");
        var added = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(added.ExitCode == SuccessExitCode, added.Output);
        Assert.Equal(Summary(translations: (1, 0, 0)), added.StdoutLines);
        Assert.Equal(baseRows, WithoutTranslationOf(question.Id, await PostgresFixture.SnapshotAsync(db)));
        await AssertTranslationAsync(db, question.Id, english);
        await using (var context = PostgresFixture.CreateContext(db))
        {
            var json = await context.Database
                .SqlQueryRaw<string>("""SELECT "OptionTexts"::text AS "Value" FROM "QuestionTranslations" WHERE "QuestionId" = {0}""", question.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(JsonValueKind.Array, JsonDocument.Parse(json).RootElement.ValueKind);
            Assert.Equal(english.Options, JsonDocument.Parse(json).RootElement.EnumerateArray().Select(text => text.GetString()));
        }

        var afterAdd = await PostgresFixture.SnapshotAsync(db);
        // Quotes, <&>, é and the dash come back from jsonb escaped differently: still not an update.
        var repeat = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(repeat.ExitCode == SuccessExitCode, repeat.Output);
        Assert.Equal(NothingChanged, repeat.StdoutLines);
        Assert.Equal(afterAdd, await PostgresFixture.SnapshotAsync(db));

        content.Replace(SqlFile, Quoted(english.Explanation), Quoted(english.Explanation + " Changed."));
        var explanationChanged = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(explanationChanged.ExitCode == SuccessExitCode, explanationChanged.Output);
        Assert.Equal(Summary(translations: (0, 1, 0)), explanationChanged.StdoutLines);
        Assert.Equal(baseRows, WithoutTranslationOf(question.Id, await PostgresFixture.SnapshotAsync(db)));
        await AssertTranslationAsync(db, question.Id, english with { Explanation = english.Explanation + " Changed." });

        content.Replace(SqlFile, Quoted(english.Options[^1]), Quoted("Last option, reworded"));
        var optionChanged = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(optionChanged.ExitCode == SuccessExitCode, optionChanged.Output);
        Assert.Equal(Summary(translations: (0, 1, 0)), optionChanged.StdoutLines);
        await AssertTranslationAsync(db, question.Id, english with
        {
            Explanation = english.Explanation + " Changed.",
            Options = [.. english.Options.Take(english.Options.Count - 1), "Last option, reworded"],
        });

        content.Write(SqlFile, original);
        var removed = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(removed.ExitCode == SuccessExitCode, removed.Output);
        Assert.Equal(Summary(translations: (0, 0, 1)), removed.StdoutLines);
        Assert.Equal(baseRows, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task Title_en_updates_only_its_catalog_entry_and_never_the_questions()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = WithUntranslatedSql();
        await ImportOkAsync(db, content.Root);
        var before = await UpdatedAtByIdAsync(db);

        content.Replace(SqlFile, SqlTitle, SqlTitle + "title_en: SQL and relational databases\n");
        var added = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(added.ExitCode == SuccessExitCode, added.Output);
        Assert.Equal(Summary(topics: (0, 1, 0)), added.StdoutLines);
        Assert.Equal([("en", "SQL and relational databases")], Pairs((await SqlTopicAsync(db)).TitleTranslations));

        content.Replace(SqlFile, "title_en: SQL and relational databases\n", "title_en: \"SQL & relational DBs — \\\"quoted\\\"\"\n");
        var changed = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(changed.ExitCode == SuccessExitCode, changed.Output);
        Assert.Equal(Summary(topics: (0, 1, 0)), changed.StdoutLines);
        Assert.Equal("SQL & relational DBs — \"quoted\"", (await SqlTopicAsync(db)).TitleTranslations["en"]);

        content.Replace(SqlFile, "\nquestions:", "\ndescription_en: Queries, joins and indexes.\nquestions:");
        var description = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(description.ExitCode == SuccessExitCode, description.Output);
        Assert.Equal(Summary(topics: (0, 1, 0)), description.StdoutLines);
        Assert.Equal([("en", "Queries, joins and indexes.")], Pairs((await SqlTopicAsync(db)).DescriptionTranslations));

        content.Replace(FieldFile, "title: IT\n", "title: IT\ntitle_en: IT (en)\n");
        content.Replace(FieldFile, "    title: Junior\n", "    title: Junior\n    title_en: Junior (en)\n");
        content.Replace(FieldFile, "    title: Backend\n", "    title: Backend\n    title_en: Backend (en)\n");
        content.Replace(FieldFile, "        title: .NET (C#)\n", "        title: .NET (C#)\n        title_en: \".NET (en)\"\n");
        var field = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(field.ExitCode == SuccessExitCode, field.Output);
        Assert.Equal(Summary(fields: (0, 1, 0), levels: (0, 1, 0), directions: (0, 1, 0), stacks: (0, 1, 0)), field.StdoutLines);

        var afterAll = await PostgresFixture.SnapshotAsync(db);
        var repeat = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(repeat.ExitCode == SuccessExitCode, repeat.Output);
        Assert.Equal(NothingChanged, repeat.StdoutLines);
        Assert.Equal(afterAll, await PostgresFixture.SnapshotAsync(db));
        Assert.Equal(before, await UpdatedAtByIdAsync(db));
        await using var context = PostgresFixture.CreateContext(db);
        var ct = TestContext.Current.CancellationToken;
        Assert.Equal("IT (en)", (await context.Fields.AsNoTracking().SingleAsync(ct)).TitleTranslations["en"]);
        Assert.Equal("Junior (en)", (await context.Levels.AsNoTracking().SingleAsync(level => level.Rank == 1, ct)).TitleTranslations["en"]);
        Assert.Equal(".NET (en)", (await context.Stacks.AsNoTracking().SingleAsync(stack => stack.Title == ".NET (C#)", ct)).TitleTranslations["en"]);
        Assert.Empty((await context.Levels.AsNoTracking().SingleAsync(level => level.Rank == 2, ct)).TitleTranslations);
    }

    [Fact]
    public async Task Draft_en_holds_back_the_question_translations_of_its_topic_without_the_flag_but_not_its_questions_or_titles()
    {
        var db = await postgres.CreateDatabaseAsync();
        var (content, english) = WithEnglishSql();
        using var _ = content;
        var sqlCount = RealTopic("sql").Questions.Count;
        var drafts = RealDraftQuestionCount();
        // The other topics' translations (translation waves) are not this test's subject, but the flag brings in those
        // of draft and draft_en topics alongside sql's.
        var (published, all) = (QuestionTranslationCount(content.Root, includeDrafts: false), QuestionTranslationCount(content.Root, includeDrafts: true));
        var withFlagOnly = all - published + sqlCount;
        await ImportOkAsync(db, content.Root);
        Assert.Equal(published, await CountAsync(db, context => context.QuestionTranslations));
        Assert.Equal(sqlCount, await CountAsync(db, context => context.QuestionTranslations.Where(t => t.QuestionId.StartsWith("sql-"))));
        var before = await UpdatedAtByIdAsync(db);

        // The emergency switch-off of a published translation (ADR 006): draft_en back on.
        content.Replace(SqlFile, "\ndescription_en: ", "\ndraft_en: true\ndescription_en: ");
        var off = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(off.ExitCode == SuccessExitCode, off.Output);
        Assert.Equal(Summary(translations: (0, 0, sqlCount)), off.StdoutLines);
        Assert.Equal(published - sqlCount, await CountAsync(db, context => context.QuestionTranslations));
        Assert.Equal(0, await CountAsync(db, context => context.QuestionTranslations.Where(t => t.QuestionId.StartsWith("sql-"))));
        Assert.Equal(before, await UpdatedAtByIdAsync(db));
        Assert.Equal(sqlCount, await CountAsync(db, context => context.Questions.Where(q => q.TopicId == "sql" && q.IsActive)));
        // draft_en does not act on the topic's own titles (ADR 006, section 1).
        Assert.Equal("SQL and relational databases", (await SqlTopicAsync(db)).TitleTranslations["en"]);
        Assert.Equal(NothingChanged, (await RunIsolatedAsync(db, null, "import", content.Root)).StdoutLines);

        var withDrafts = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(withDrafts.ExitCode == SuccessExitCode, withDrafts.Output);
        Assert.Equal(Summary(questions: (drafts, 0, 0), translations: (withFlagOnly, 0, 0)), withDrafts.StdoutLines);
        Assert.Equal(all, await CountAsync(db, context => context.QuestionTranslations));
        await AssertTranslationAsync(db, "sql-001", english["sql-001"]);
        Assert.Equal(NothingChanged, (await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts)).StdoutLines);

        var offAgain = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(offAgain.ExitCode == SuccessExitCode, offAgain.Output);
        Assert.Equal(Summary(questions: (0, 0, drafts), translations: (0, 0, withFlagOnly)), offAgain.StdoutLines);
        Assert.Equal(published - sqlCount, await CountAsync(db, context => context.QuestionTranslations));
        Assert.Equal(0, await CountAsync(db, context => context.QuestionTranslations.Where(t => t.QuestionId.StartsWith("sql-"))));
    }

    [Fact]
    public async Task Draft_topic_without_the_flag_deactivates_its_questions_and_removes_their_translations()
    {
        var db = await postgres.CreateDatabaseAsync();
        var (content, english) = WithEnglishSql();
        using var _ = content;
        var sqlCount = RealTopic("sql").Questions.Count;
        // Published translations of the other topics stay through all of this (translation waves).
        var others = QuestionTranslationCount(content.Root, includeDrafts: false) - sqlCount;
        var translated = content.Read(SqlFile);
        await ImportOkAsync(db, content.Root);
        Assert.Equal(others + sqlCount, await CountAsync(db, context => context.QuestionTranslations));

        content.Replace(SqlFile, "\nquestions:", "\ndraft: true\nquestions:");
        var off = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(off.ExitCode == SuccessExitCode, off.Output);
        Assert.Equal(Summary(questions: (0, 0, sqlCount), translations: (0, 0, sqlCount)), off.StdoutLines);
        Assert.Equal(others, await CountAsync(db, context => context.QuestionTranslations));
        Assert.Equal(0, await CountAsync(db, context => context.QuestionTranslations.Where(t => t.QuestionId.StartsWith("sql-"))));
        Assert.Equal(sqlCount, await CountAsync(db, context => context.Questions.Where(q => q.TopicId == "sql" && !q.IsActive)));

        content.Write(SqlFile, translated);
        var back = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(back.ExitCode == SuccessExitCode, back.Output);
        Assert.Equal(Summary(questions: (0, sqlCount, 0), translations: (sqlCount, 0, 0)), back.StdoutLines);
        await AssertTranslationAsync(db, "sql-002", english["sql-002"]);
    }

    [Fact]
    public async Task Question_removed_from_the_file_is_deactivated_and_loses_its_translation_until_it_returns()
    {
        var db = await postgres.CreateDatabaseAsync();
        var (content, english) = WithEnglishSql();
        using var _ = content;
        var last = RealTopic("sql").Questions[^1].Id;
        // Published translations of all the other questions stay (translation waves).
        var others = QuestionTranslationCount(content.Root, includeDrafts: false) - 1;
        var translated = content.Read(SqlFile);
        await ImportOkAsync(db, content.Root);

        content.Write(SqlFile, translated[..translated.IndexOf($"  - id: {last}\n", StringComparison.Ordinal)]);
        var removed = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(removed.ExitCode == SuccessExitCode, removed.Output);
        Assert.Equal(Summary(questions: (0, 0, 1), translations: (0, 0, 1)), removed.StdoutLines);
        Assert.False((await QuestionAsync(db, last)).IsActive);
        Assert.Equal(others, await CountAsync(db, context => context.QuestionTranslations));
        Assert.Equal(0, await CountAsync(db, context => context.QuestionTranslations.Where(t => t.QuestionId == last)));

        content.Write(SqlFile, translated);
        var returned = await RunIsolatedAsync(db, null, "import", content.Root);

        Assert.True(returned.ExitCode == SuccessExitCode, returned.Output);
        Assert.Equal(Summary(questions: (0, 1, 0), translations: (1, 0, 0)), returned.StdoutLines);
        await AssertTranslationAsync(db, last, english[last]);
    }

    [Fact]
    public async Task Database_failure_on_a_translation_insert_rolls_back_questions_translations_and_titles()
    {
        var db = await postgres.CreateDatabaseAsync();
        using var content = WithUntranslatedSql();
        var original = content.Read(SqlFile);
        TranslateQuestion(content, "sql-001", "one");
        TranslateQuestion(content, "sql-002", "two");
        // sql holds English back (rule 12), so its translations are written only with --include-drafts.
        await ImportOkAsync(db, content.Root, IncludeDrafts);
        await PostgresFixture.ExecuteAsync(db, """
            CREATE FUNCTION fail_import() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'test: translation of % refused', NEW."QuestionId"; END $$;
            CREATE TRIGGER fail_import BEFORE INSERT ON "QuestionTranslations"
            FOR EACH ROW WHEN (NEW."QuestionId" = 'sql-003') EXECUTE FUNCTION fail_import();
            """);
        var before = await PostgresFixture.SnapshotAsync(db);

        // Every kind of write at once: a base question update, a translation update, delete and the failing insert,
        // a topic title and a new level with a question on it.
        content.Write(SqlFile, original);
        content.Replace(SqlFile, "`WHERE` відкидає рядки ще до", "`WHERE` відсікає рядки ще до");
        TranslateQuestion(content, "sql-001", "one, changed");
        TranslateQuestion(content, "sql-003", "three");
        content.Replace(SqlFile, SqlTitle, SqlTitle + "title_en: SQL and relational databases\n");
        content.Replace(FieldFile, "  - id: senior\n    title: Senior", ArchitectLevel);
        content.Write(SqlFile, content.Read(SqlFile) + ArchitectQuestion);
        var run = await RunIsolatedAsync(db, null, "import", content.Root, IncludeDrafts);

        Assert.True(run.ExitCode == DatabaseErrorExitCode, run.Output);
        Assert.Contains("Помилка бази даних під час імпорту", run.Stderr);
        Assert.Contains("translation of sql-003 refused", run.Stderr);
        Assert.Equal(before, await PostgresFixture.SnapshotAsync(db));
    }

    [Fact]
    public async Task ContentTranslations_migration_on_an_imported_database_gives_empty_translations_and_a_no_op_import()
    {
        // ADR 006, section 2: the migration only adds; rows written before it mean "no translation", as on Neon.
        // Neon got the content before it had any English, while the real one carries its own since PRP-030.
        var db = await postgres.CreateDatabaseAsync();
        using var untranslated = new RealContentCopy();
        EnglishTranslation.RemoveFrom(untranslated.Root);
        var content = untranslated.Root;
        await ImportOkAsync(db, content, IncludeDrafts);
        var imported = await PostgresFixture.SnapshotAsync(db);
        var ct = TestContext.Current.CancellationToken;

        await using (var context = PostgresFixture.CreateContext(db))
        {
            var migrator = context.GetService<IMigrator>();
            var initial = context.Database.GetMigrations().First();
            await migrator.MigrateAsync(initial, cancellationToken: ct);
            Assert.Equal(new[] { initial }, await context.Database.GetAppliedMigrationsAsync(ct));
            await migrator.MigrateAsync(cancellationToken: ct);
            Assert.Empty(await context.Database.GetPendingMigrationsAsync(ct));
        }

        Assert.Equal(imported, await PostgresFixture.SnapshotAsync(db));
        await using (var context = PostgresFixture.CreateContext(db))
        {
            Assert.Equal(0, await context.Database
                .SqlQueryRaw<int>("""
                    SELECT count(*)::int AS "Value" FROM (
                        SELECT "TitleTranslations" AS t FROM "Fields" UNION ALL SELECT "TitleTranslations" FROM "Levels"
                        UNION ALL SELECT "TitleTranslations" FROM "Directions" UNION ALL SELECT "TitleTranslations" FROM "Stacks"
                        UNION ALL SELECT "TitleTranslations" FROM "Topics" UNION ALL SELECT "DescriptionTranslations" FROM "Topics") x
                    WHERE t <> '{{}}'::jsonb
                    """)
                .SingleAsync(ct));
            Assert.Equal(0, await context.QuestionTranslations.CountAsync(ct));
        }

        var run = await RunIsolatedAsync(db, null, "import", content, IncludeDrafts);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(NothingChanged, run.StdoutLines);

        // Then the deploy of PRP-030 onto it: the real content's topic titles land, and no question changes.
        var real = await RunIsolatedAsync(db, null, "import", RealContentDirectory(), IncludeDrafts);

        Assert.True(real.ExitCode == SuccessExitCode, real.Output);
        Assert.Contains(Counts("Питання", (0, 0, 0)), real.StdoutLines);
        Assert.Contains(Counts("Теми", (0, RealTopicsWithEnglishCount(), 0)), real.StdoutLines);
    }

    /// <summary>Topics whose file has a top-level title_en or description_en, from the raw lines.</summary>
    private static int RealTopicsWithEnglishCount() =>
        Directory.GetFiles(Path.Combine(RealContentDirectory(), "it", "topics"), "*.yaml")
            .Count(path => File.ReadLines(path).Any(line =>
                line.StartsWith("title_en:", StringComparison.Ordinal) || line.StartsWith("description_en:", StringComparison.Ordinal)));

    /// <summary>
    /// A copy of the real content whose sql.yaml has no English yet, where these tests add their own: sql carries
    /// a title_en since PRP-030, and translation waves add the rest of it. Without English sql holds it back with
    /// draft_en: true (rule 12), so its question translations are imported only with --include-drafts.
    /// </summary>
    private static RealContentCopy WithUntranslatedSql()
    {
        var content = new RealContentCopy();
        EnglishTranslation.RemoveFrom(Path.Combine(content.Root, SqlFile));
        return content;
    }

    /// <summary>
    /// A copy whose sql.yaml is translated whole, by these tests' own texts, and published in English (no draft_en):
    /// since rule 12 the only sql whose translations a default import stores. The English of each question by id.
    /// </summary>
    private static (RealContentCopy Content, Dictionary<string, English> English) WithEnglishSql()
    {
        var content = WithUntranslatedSql();
        var english = RealTopic("sql").Questions.ToDictionary(question => question.Id, question => TranslateQuestion(content, question.Id, question.Id));
        content.Replace(SqlFile, SqlTitle, SqlTitle + "title_en: SQL and relational databases\n");
        content.Replace(SqlFile, "\ndraft_en: true\n", "\ndescription_en: Queries, joins and indexes.\n");
        return (content, english);
    }

    /// <summary>The English texts a test gave one question; the option texts are in file order.</summary>
    private sealed record English(string Text, IReadOnlyList<string> Options, string Explanation);

    /// <summary>
    /// Gives question <paramref name="questionId"/> of sql.yaml a full English translation, line by line beside its
    /// base keys (the base lines stay byte for byte). Code lines sit deeper than any matched key, so they are never touched.
    /// The texts carry quotes, <c>&lt;&amp;&gt;</c>, accents and a dash, which jsonb stores escaped its own way.
    /// </summary>
    private static English TranslateQuestion(RealContentCopy content, string questionId, string tag)
    {
        var yaml = content.Read(SqlFile);
        var start = yaml.IndexOf($"  - id: {questionId}\n", StringComparison.Ordinal);
        Assert.True(start >= 0, $"{SqlFile} has no {questionId}.");
        var next = yaml.IndexOf("\n  - id: ", start + 1, StringComparison.Ordinal);
        var end = next < 0 ? yaml.Length : next;

        var text = $"[{tag}] What does \"WHERE\" do — <&> é?";
        var explanation = $"[{tag}] Because — `WHERE` runs first, <b>ü</b> & so on.";
        var options = new List<string>();
        var output = new List<string>();
        foreach (var line in yaml[start..end].Split('\n'))
        {
            if (line.StartsWith("    text: ", StringComparison.Ordinal))
            {
                output.Add($"    text_en: {Quoted(text)}");
                output.Add(line);
            }
            else if (line.StartsWith("      - text: ", StringComparison.Ordinal))
            {
                var option = $"[{tag}] option {options.Count + 1}: \"quoted\" <&>";
                options.Add(option);
                output.Add($"      - text_en: {Quoted(option)}");
                output.Add($"        text: {line["      - text: ".Length..]}");
            }
            else if (line.StartsWith("    explanation:", StringComparison.Ordinal))
            {
                output.Add($"    explanation_en: {Quoted(explanation)}");
                output.Add(line);
            }
            else
            {
                output.Add(line);
            }
        }

        Assert.NotEmpty(options);
        content.Write(SqlFile, string.Concat(yaml[..start], string.Join('\n', output), yaml[end..]));
        return new English(text, options, explanation);
    }

    /// <summary>A YAML double-quoted scalar.</summary>
    private static string Quoted(string text) => $"\"{text.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";

    /// <summary>A translations dictionary as pairs sorted by language, so comparing two ignores the key order.</summary>
    private static List<(string Language, string Text)> Pairs(IReadOnlyDictionary<string, string> texts) =>
        texts.Select(pair => (pair.Key, pair.Value)).OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();

    // Real content already carries translations of other topics, so only the one row under test may differ.
    private static List<string> WithoutTranslationOf(string questionId, IEnumerable<string> rows) =>
        rows.Where(row => !row.StartsWith($"QuestionTranslations {{\"QuestionId\":\"{questionId}\"", StringComparison.Ordinal)).ToList();

    private static async Task AssertTranslationAsync(string connectionString, string questionId, English expected)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        var translation = await context.QuestionTranslations.AsNoTracking()
            .SingleAsync(row => row.QuestionId == questionId, TestContext.Current.CancellationToken);
        Assert.Equal(("en", expected.Text, expected.Explanation), (translation.Language, translation.Text, translation.Explanation));
        Assert.Equal(expected.Options, translation.OptionTexts);
    }

    private static async Task<Topic> SqlTopicAsync(string connectionString)
    {
        await using var context = PostgresFixture.CreateContext(connectionString);
        return await context.Topics.AsNoTracking().SingleAsync(topic => topic.Id == "sql", TestContext.Current.CancellationToken);
    }
}

using System.Text.RegularExpressions;
using Prepround.Tests.Content;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// <c>validate &lt;dir&gt;</c> through the real process (ADR 002): 0 with a summary on stdout,
/// 1 with "file: id: message" lines on stderr for every rule 1–9 and rule 10 of ADR 004.
/// </summary>
public partial class ValidateCommandTests
{
    /// <summary>One way to break the fixture for each rule of ADR 002 and ADR 004, keyed by the name used in the theory.</summary>
    private static readonly Dictionary<string, Action<ContentFixture>> Breakages = new()
    {
        ["rule-1-question-id-not-kebab"] = c => c.Replace(ContentFixture.AlphaFile, "id: alpha-001", "id: Alpha_001"),
        ["rule-1-level-id-not-kebab"] = c => c.Replace(ContentFixture.FieldFile, "id: middle", "id: Middle"),
        ["rule-2-question-id-in-two-files"] = c => c.Replace(ContentFixture.BetaFile, "questions: []", """
            questions:
              - id: alpha-001
                level: junior
                type: single
                text: Копія id з іншого файлу?
                options:
                  - text: Так
                    correct: true
                  - text: Ні
                explanation: Id питань унікальні в усьому контенті.
            """),
        ["rule-2-stack-id-twice"] = c => c.Replace(ContentFixture.FieldFile, "        topics: [alpha, beta]", """
                    topics: [alpha, beta]
                  - id: dotnet
                    title: Ще раз .NET
                    topics: [alpha]
            """),
        ["rule-3-file-name-differs-from-id"] = c => c.Move(ContentFixture.BetaFile, "it/topics/beta-old.yaml"),
        ["rule-3-folder-differs-from-field-id"] = c => c.Replace(ContentFixture.FieldFile, "id: it", "id: tech"),
        ["rule-4-stack-topic-missing"] = c => c.Replace(ContentFixture.FieldFile, "[alpha, beta]", "[alpha, beta, gamma]"),
        ["rule-4-stack-topic-twice"] = c => c.Replace(ContentFixture.FieldFile, "[alpha, beta]", "[alpha, beta, alpha]"),
        ["rule-5-unknown-level"] = c => c.Replace(ContentFixture.AlphaFile, "level: junior", "level: architect"),
        ["rule-6-unknown-type"] = c => c.Replace(ContentFixture.AlphaFile, "type: single", "type: singel"),
        ["rule-6-single-with-two-correct"] = c => c.Replace(ContentFixture.AlphaFile, "  - text: Ні", "  - text: Ні\n        correct: true"),
        ["rule-6-multiple-with-one-correct"] = c => c.Replace(ContentFixture.AlphaFile, "  - text: Другий\n        correct: true", "  - text: Другий"),
        ["rule-6-multiple-all-correct"] = c => c.Replace(ContentFixture.AlphaFile, "  - text: Третій", "  - text: Третій\n        correct: true"),
        ["rule-7-one-option"] = c => c.Replace(ContentFixture.AlphaFile, "      - text: Ні\n", ""),
        ["rule-7-nine-options"] = c => c.Replace(ContentFixture.AlphaFile, "      - text: Ні\n",
            string.Concat(Enumerable.Range(1, 8).Select(i => $"      - text: Ні {i}\n"))),
        ["rule-7-duplicate-option"] = c => c.Replace(ContentFixture.AlphaFile, "      - text: Ні\n", "      - text: Так\n"),
        ["rule-8-empty-explanation"] = c => c.Replace(ContentFixture.AlphaFile, "explanation: Бо так.", "explanation: \"\""),
        ["rule-8-code-lang-not-kebab"] = c => c.Replace(ContentFixture.AlphaFile, "lang: csharp", "lang: C#"),
        ["rule-9-option-typo-corect"] = c => c.Replace(ContentFixture.AlphaFile, "correct: true", "corect: true"),
        ["rule-9-unknown-key-in-field"] = c => c.Replace(ContentFixture.FieldFile, "title: IT", "title: IT\ntitel: IT"),
        ["yaml-syntax-error"] = c => c.Replace(ContentFixture.AlphaFile, "  - text: Ні", "  - text: [Ні"),
        ["rule-10-empty-sources"] = c => WithAlphaSources(c, "sources: []"),
        ["rule-10-http-source"] = c => WithAlphaSources(c, "sources: [http://www.postgresql.org/docs/18/ddl-constraints.html]"),
        ["rule-10-relative-source"] = c => WithAlphaSources(c, "sources: [www.postgresql.org/docs/18/]"),
        ["rule-10-repeated-source"] = c => WithAlphaSources(c,
            "sources: [https://www.postgresql.org/docs/18/, https://www.postgresql.org/docs/18/]"),
        ["rule-10-source-with-space"] = c => WithAlphaSources(c,
            "sources: [\"https://www.postgresql.org/docs/18/ddl constraints.html\"]"),
        ["rule-10-sources-not-a-list"] = c => WithAlphaSources(c, "sources: https://www.postgresql.org/docs/18/"),
        ["draft-not-a-boolean"] = c => c.Replace(ContentFixture.AlphaFile, "description: Перша тема.", "description: Перша тема.\ndraft: maybe"),
        ["draft-not-a-scalar"] = c => c.Replace(ContentFixture.AlphaFile, "description: Перша тема.", "description: Перша тема.\ndraft: [true]"),
        // PRP-025: translation keys (ADR 006) and rule 11.
        ["draft-en-not-a-boolean"] = c =>
        {
            c.PublishInEnglish(ContentFixture.AlphaFile);
            c.Replace(ContentFixture.AlphaFile, "description: Перша тема.", "description: Перша тема.\ndraft_en: так");
        },
        ["rule-9-text-de"] = c => c.Replace(ContentFixture.AlphaFile, "    level: junior", "    level: junior\n    text_de: x"),
        ["rule-9-title-EN"] = c => c.Replace(ContentFixture.AlphaFile, "title: Alpha", "title: Alpha\ntitle_EN: x"),
        ["rule-9-code-source-en"] = c => c.Replace(ContentFixture.AlphaFile, "      lang: csharp", "      lang: csharp\n      source_en: x"),
        ["rule-11-incomplete-translation"] = c =>
        {
            TranslatedFixture.TranslateAlphaAndItsFirstQuestion(c);
            c.Replace(ContentFixture.AlphaFile, $"        text_en: \"{TranslatedFixture.SecondOption}\"\n", "");
        },
        ["rule-11-blank-text-en"] = c =>
        {
            TranslatedFixture.TranslateAlphaAndItsFirstQuestion(c);
            c.Replace(ContentFixture.AlphaFile, $"text_en: {TranslatedFixture.QuestionText}", "text_en: '   '");
        },
        ["rule-11-repeated-option-en"] = c =>
        {
            TranslatedFixture.TranslateAlphaAndItsFirstQuestion(c);
            c.Replace(ContentFixture.AlphaFile, $"text_en: \"{TranslatedFixture.SecondOption}\"", $"text_en: \" {TranslatedFixture.FirstOption} \"");
        },
        ["rule-11-blank-level-title-en"] = c =>
        {
            TranslatedFixture.TranslateField(c);
            c.Replace(ContentFixture.FieldFile, $"title_en: {TranslatedFixture.LevelTitle}", "title_en: \"\"");
        },
        // PRP-031: rule 12, a topic published in English has all of it.
        ["rule-12-published-question-without-en"] = c =>
        {
            TranslatedFixture.TranslateAlphaAndItsFirstQuestion(c);
            c.PublishInEnglish(ContentFixture.AlphaFile);
        },
        ["rule-12-published-without-title-en"] = c => c.PublishInEnglish(ContentFixture.BetaFile),
        ["rule-12-draft-en-false"] = c => c.Replace(ContentFixture.BetaFile, ContentFixture.DraftEn, "draft_en: false"),
        ["rule-12-published-without-description-en"] = c =>
        {
            TranslatedFixture.TranslateAlphaAndItsFirstQuestion(c);
            c.Replace(ContentFixture.AlphaFile, $"description_en: {TranslatedFixture.TopicDescription}\n", "");
            c.PublishInEnglish(ContentFixture.AlphaFile);
        },
    };

    [Fact]
    public async Task Real_content_is_valid_and_summarised_per_field()
    {
        var run = await RunAsync("validate", RealContentDirectory());

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Empty(run.Stderr);
        var line = Assert.Single(run.StdoutLines, output => output.StartsWith("it: ", StringComparison.Ordinal));
        // PRP-025: complete question translations append ", перекладів <lang>: N" after the drafts, which translation
        // waves will bring into the real content; translated topic titles alone (PRP-030) never do.
        var summary = TranslationsSuffix().Replace(line, "");
        Assert.Matches(SummaryLine(), summary);
        // validate counts every complete translation, drafts included; the raw lines of the files say how many.
        var translated = QuestionTranslationCount(RealContentDirectory(), includeDrafts: true);
        Assert.Equal(translated > 0 ? $", перекладів en: {translated}" : "", line[summary.Length..]);
        // PRP-023: the total and the drafts come from the raw lines of the files; the suffix appears only with drafts.
        var drafts = RealDraftQuestionCount();
        Assert.EndsWith(
            drafts > 0 ? $", питань {RealQuestionCount()}, з них у чернетках {drafts}" : $", питань {RealQuestionCount()}",
            summary);
    }

    [Fact]
    public async Task Draft_topic_is_counted_in_the_summary_and_draft_false_prints_the_old_summary()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", "description: Перша тема.\ndraft: true");

        var draft = await RunAsync("validate", content.Root);

        content.Replace(ContentFixture.AlphaFile, "draft: true", "draft: false");
        var notDraft = await RunAsync("validate", content.Root);

        Assert.True(draft.ExitCode == SuccessExitCode, draft.Output);
        Assert.Equal([$"{ContentFixture.Summary}, з них у чернетках 2"], draft.StdoutLines);
        Assert.True(notDraft.ExitCode == SuccessExitCode, notDraft.Output);
        Assert.Equal([ContentFixture.Summary], notDraft.StdoutLines);
    }

    [Fact]
    public async Task Fully_translated_question_is_counted_in_the_summary_after_the_drafts()
    {
        // PRP-025: the suffix names the language and counts only complete translations of questions.
        using var content = new ContentFixture();
        TranslatedFixture.TranslateField(content);
        TranslatedFixture.TranslateAlphaAndItsFirstQuestion(content);

        var translated = await RunAsync("validate", content.Root);

        content.Replace(ContentFixture.AlphaFile, ContentFixture.DraftEn, $"{ContentFixture.DraftEn}\ndraft: true");
        var draft = await RunAsync("validate", content.Root);

        Assert.True(translated.ExitCode == SuccessExitCode, translated.Output);
        Assert.Equal([$"{ContentFixture.Summary}, перекладів en: 1"], translated.StdoutLines);
        Assert.True(draft.ExitCode == SuccessExitCode, draft.Output);
        Assert.Equal([$"{ContentFixture.Summary}, з них у чернетках 2, перекладів en: 1"], draft.StdoutLines);
    }

    [Fact]
    public async Task Translated_titles_and_draft_en_without_a_translated_question_print_the_old_summary()
    {
        using var content = new ContentFixture();
        TranslatedFixture.TranslateField(content);
        // alpha keeps the draft_en of the fixture.
        content.Replace(ContentFixture.AlphaFile, "title: Alpha", "title: Alpha\ntitle_en: Alpha");

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal([ContentFixture.Summary], run.StdoutLines);
    }

    [Fact]
    public async Task Real_content_with_a_complete_english_translation_counts_every_question_as_translated()
    {
        // The real content has English of its own since PRP-030; "before" is the content without any of it.
        using var content = new RealContentCopy();
        EnglishTranslation.RemoveFrom(content.Root);
        var before = await RunAsync("validate", content.Root);
        EnglishTranslation.AddTo(content.Root);

        var after = await RunAsync("validate", content.Root);

        Assert.True(after.ExitCode == SuccessExitCode, after.Output);
        var summary = Assert.Single(before.StdoutLines);
        Assert.Equal([$"{summary}, перекладів en: {RealQuestionCount()}"], after.StdoutLines);
    }

    [Fact]
    public async Task Draft_topic_is_checked_by_the_same_rules_as_any_other_topic()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", "description: Перша тема.\ndraft: true");
        Breakages["rule-6-unknown-type"](content);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains(run.StderrLines, line =>
            line.StartsWith($"{ContentFixture.AlphaFile}: alpha-001: ", StringComparison.Ordinal) && line.Contains("singel", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Valid_content_prints_the_summary_to_stdout_and_exits_with_0()
    {
        using var content = new ContentFixture();

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal([ContentFixture.Summary], run.StdoutLines);
        Assert.Empty(run.Stderr);
    }

    [Fact]
    public async Task New_level_added_only_in_yaml_is_accepted_without_code_changes()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, "    title: Senior", "    title: Senior\n  - id: architect\n    title: Architect");
        content.Replace(ContentFixture.AlphaFile, "level: senior", "level: architect");

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(["it: рівнів 4, напрямів 1, стеків 1, тем 2, питань 2"], run.StdoutLines);
    }

    [Fact]
    public async Task Question_with_https_sources_on_a_level_added_only_in_yaml_is_valid()
    {
        // ADR 004: sources are optional, so the untouched alpha-002 stays valid next to alpha-001 with sources.
        using var content = new ContentFixture();
        content.Replace(ContentFixture.FieldFile, "    title: Senior", "    title: Senior\n  - id: architect\n    title: Architect");
        content.Replace(ContentFixture.AlphaFile, "level: junior", """
            level: architect
                sources:
                  - https://www.postgresql.org/docs/18/ddl-constraints.html#DDL-CONSTRAINTS-PRIMARY-KEYS
                  - https://www.postgresql.org/docs/18/queries-table-expressions.html
            """);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(["it: рівнів 4, напрямів 1, стеків 1, тем 2, питань 2"], run.StdoutLines);
        Assert.Empty(run.Stderr);
    }

    [Fact]
    public async Task Every_field_gets_its_own_summary_line()
    {
        using var content = new ContentFixture();
        content.Write("med/field.yaml", """
            id: med
            title: Медицина
            levels:
              - id: aspirant
                title: Аспірант
              - id: phd
                title: Кандидат наук
            """);
        content.Write("med/topics/anatomy.yaml", """
            id: anatomy
            title: Анатомія
            draft_en: true
            questions:
              - id: anatomy-001
                level: phd
                type: single
                text: Скільки камер у серці людини?
                options:
                  - text: "4"
                    correct: true
                  - text: "3"
                explanation: Два передсердя і два шлуночки.
            """);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal([ContentFixture.Summary, "med: рівнів 2, напрямів 0, стеків 0, тем 1, питань 1"], run.StdoutLines);
    }

    [Theory]
    [InlineData("rule-1-question-id-not-kebab", ContentFixture.AlphaFile, "Alpha_001", "kebab-case")]
    [InlineData("rule-1-level-id-not-kebab", ContentFixture.FieldFile, "Middle", "kebab-case")]
    [InlineData("rule-2-question-id-in-two-files", ContentFixture.BetaFile, "alpha-001", ContentFixture.AlphaFile)]
    [InlineData("rule-2-stack-id-twice", ContentFixture.FieldFile, "dotnet", "повторюється")]
    [InlineData("rule-3-file-name-differs-from-id", "it/topics/beta-old.yaml", "beta", "beta-old.yaml")]
    [InlineData("rule-3-folder-differs-from-field-id", ContentFixture.FieldFile, "tech", "«it»")]
    [InlineData("rule-4-stack-topic-missing", ContentFixture.FieldFile, "dotnet", "gamma")]
    [InlineData("rule-4-stack-topic-twice", ContentFixture.FieldFile, "dotnet", "«alpha» повторюється")]
    [InlineData("rule-5-unknown-level", ContentFixture.AlphaFile, "alpha-001", "architect")]
    [InlineData("rule-6-unknown-type", ContentFixture.AlphaFile, "alpha-001", "singel")]
    [InlineData("rule-6-single-with-two-correct", ContentFixture.AlphaFile, "alpha-001", "рівно 1")]
    [InlineData("rule-6-multiple-with-one-correct", ContentFixture.AlphaFile, "alpha-002", "щонайменше 2")]
    [InlineData("rule-6-multiple-all-correct", ContentFixture.AlphaFile, "alpha-002", "неправильний")]
    [InlineData("rule-7-one-option", ContentFixture.AlphaFile, "alpha-001", "від 2 до 8, а їх 1")]
    [InlineData("rule-7-nine-options", ContentFixture.AlphaFile, "alpha-001", "від 2 до 8, а їх 9")]
    [InlineData("rule-7-duplicate-option", ContentFixture.AlphaFile, "alpha-001", "«Так» повторюється")]
    [InlineData("rule-8-empty-explanation", ContentFixture.AlphaFile, "alpha-001", "explanation")]
    [InlineData("rule-8-code-lang-not-kebab", ContentFixture.AlphaFile, "alpha-002", "C#")]
    [InlineData("rule-9-option-typo-corect", ContentFixture.AlphaFile, "alpha-001", "«corect» у рядку 11")]
    [InlineData("rule-9-unknown-key-in-field", ContentFixture.FieldFile, "it", "«titel»")]
    [InlineData("yaml-syntax-error", ContentFixture.AlphaFile, "alpha", "Синтаксична помилка YAML")]
    [InlineData("rule-10-empty-sources", ContentFixture.AlphaFile, "alpha-001", "Порожній список джерел (sources)")]
    [InlineData("rule-10-http-source", ContentFixture.AlphaFile, "alpha-001", "«http://www.postgresql.org/docs/18/ddl-constraints.html»")]
    [InlineData("rule-10-relative-source", ContentFixture.AlphaFile, "alpha-001", "«www.postgresql.org/docs/18/»")]
    [InlineData("rule-10-repeated-source", ContentFixture.AlphaFile, "alpha-001", "«https://www.postgresql.org/docs/18/» повторюється")]
    [InlineData("rule-10-source-with-space", ContentFixture.AlphaFile, "alpha-001", "без пробілів")]
    [InlineData("rule-10-sources-not-a-list", ContentFixture.AlphaFile, "alpha-001", "«sources» у рядку 7 має бути списком")]
    [InlineData("draft-not-a-boolean", ContentFixture.AlphaFile, "alpha", "«draft» у рядку 4 має бути true або false")]
    [InlineData("draft-not-a-scalar", ContentFixture.AlphaFile, "alpha", "«draft» у рядку 4 має бути текстом")]
    [InlineData("draft-en-not-a-boolean", ContentFixture.AlphaFile, "alpha", "«draft_en» у рядку 4 має бути true або false")]
    [InlineData("rule-9-text-de", ContentFixture.AlphaFile, "alpha-001", "«text_de» у рядку 7")]
    [InlineData("rule-9-title-EN", ContentFixture.AlphaFile, "alpha", "«title_EN» у рядку 3")]
    [InlineData("rule-9-code-source-en", ContentFixture.AlphaFile, "alpha-002", "«source_en» у рядку 20")]
    [InlineData("rule-11-incomplete-translation", ContentFixture.AlphaFile, "alpha-001", "Неповний переклад мовою en: варіант 2: немає text_en.")]
    [InlineData("rule-11-blank-text-en", ContentFixture.AlphaFile, "alpha-001", "Порожній текст питання (text_en).")]
    [InlineData("rule-11-repeated-option-en", ContentFixture.AlphaFile, "alpha-001", "Варіант «Yes» повторюється (text_en).")]
    [InlineData("rule-11-blank-level-title-en", ContentFixture.FieldFile, "junior", "Порожня назва рівня (title_en).")]
    [InlineData("rule-12-published-question-without-en", ContentFixture.AlphaFile, "alpha-002", "Тема опублікована мовою en, а перекладу питання цією мовою немає.")]
    [InlineData("rule-12-published-without-title-en", ContentFixture.BetaFile, "beta", "Тема опублікована мовою en, а title_en немає.")]
    [InlineData("rule-12-draft-en-false", ContentFixture.BetaFile, "beta", "Тема опублікована мовою en, а title_en немає.")]
    [InlineData("rule-12-published-without-description-en", ContentFixture.AlphaFile, "alpha", "Тема опублікована мовою en, а description_en немає.")]
    public async Task Broken_content_is_reported_as_file_id_message_on_stderr_and_exits_with_1(
        string breakage, string file, string id, string messageFragment)
    {
        using var content = new ContentFixture();
        Breakages[breakage](content);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains(run.StderrLines, line =>
            line.StartsWith($"{file}: {id}: ", StringComparison.Ordinal)
            && line.Contains(messageFragment, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Every_problem_is_listed_and_counted_not_only_the_first()
    {
        using var content = new ContentFixture();
        Breakages["rule-6-unknown-type"](content);
        Breakages["rule-4-stack-topic-missing"](content);

        var run = await RunAsync("validate", content.Root);

        Assert.Equal(ContentErrorExitCode, run.ExitCode);
        Assert.Contains(run.StderrLines, line => line.StartsWith($"{ContentFixture.AlphaFile}: alpha-001: ", StringComparison.Ordinal));
        Assert.Contains(run.StderrLines, line => line.StartsWith($"{ContentFixture.FieldFile}: dotnet: ", StringComparison.Ordinal));
        Assert.Equal("Помилок у контенті: 2.", run.StderrLines[^1]);
    }

    [Fact]
    public async Task Root_without_any_field_folder_is_a_content_error()
    {
        var empty = Directory.CreateTempSubdirectory("prepround-empty-");
        try
        {
            var run = await RunAsync("validate", empty.FullName);

            Assert.Equal(ContentErrorExitCode, run.ExitCode);
            Assert.Empty(run.Stdout);
        }
        finally
        {
            empty.Delete(recursive: true);
        }
    }

    /// <summary>Puts one "sources" line right after "level" of alpha-001, which is line 7 of the fixture.</summary>
    private static void WithAlphaSources(ContentFixture content, string sourcesLine) =>
        content.Replace(ContentFixture.AlphaFile, "    level: junior", $"    level: junior\n    {sourcesLine}");

    // The drafts suffix (PRP-023) is the only optional part, and only with a positive count.
    [GeneratedRegex(@"^[a-z0-9-]+: рівнів \d+, напрямів \d+, стеків \d+, тем \d+, питань \d+(, з них у чернетках [1-9]\d*)?$")]
    private static partial Regex SummaryLine();

    [GeneratedRegex(@"(, перекладів [a-z]+: [1-9]\d*)+$")]
    private static partial Regex TranslationsSuffix();
}

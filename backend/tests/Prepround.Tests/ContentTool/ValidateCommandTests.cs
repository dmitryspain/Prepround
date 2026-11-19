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
    };

    [Fact]
    public async Task Real_content_is_valid_and_summarised_per_field()
    {
        var run = await RunAsync("validate", RealContentDirectory());

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Empty(run.Stderr);
        Assert.Contains(run.StdoutLines, line => SummaryLine().IsMatch(line) && line.StartsWith("it: ", StringComparison.Ordinal));
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

    [GeneratedRegex(@"^[a-z0-9-]+: рівнів \d+, напрямів \d+, стеків \d+, тем \d+, питань \d+$")]
    private static partial Regex SummaryLine();
}

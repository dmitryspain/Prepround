using System.Diagnostics;
using System.Globalization;
using System.Text;
using Prepround.Application.Content;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// Runs the real ContentTool process: its contract is the exit code and what goes to stdout vs stderr,
/// which an in-process call of Program would not show.
/// </summary>
internal static class ContentToolProcess
{
    public const int SuccessExitCode = 0;
    public const int ContentErrorExitCode = 1;
    public const int UsageErrorExitCode = 2;
    public const int DatabaseErrorExitCode = 3;

    /// <summary>
    /// Runs a command with a private, empty HOME (or <paramref name="home"/>) and no APPDATA, so the machine's
    /// user-secrets never take part, and with <c>ConnectionStrings__Prepround</c> set as given (null removes it).
    /// </summary>
    public static async Task<ContentToolRun> RunIsolatedAsync(string? connectionString, string? home, params string[] args)
    {
        var privateHome = home is null ? Directory.CreateTempSubdirectory("prepround-home-") : null;
        try
        {
            return await RunWithEnvironmentAsync(
                new Dictionary<string, string?>
                {
                    ["HOME"] = home ?? privateHome!.FullName,
                    ["APPDATA"] = null,
                    ["ConnectionStrings__Prepround"] = connectionString,
                },
                args);
        }
        finally
        {
            privateHome?.Delete(recursive: true);
        }
    }

    public static Task<ContentToolRun> RunAsync(params string[] args) =>
        RunWithEnvironmentAsync(new Dictionary<string, string?>(), args);

    /// <summary>Runs with environment overrides; a null value removes the variable from the child process.</summary>
    public static async Task<ContentToolRun> RunWithEnvironmentAsync(
        IReadOnlyDictionary<string, string?> environment, params string[] args)
    {
        var ct = TestContext.Current.CancellationToken;
        var startInfo = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "Prepround.ContentTool.dll"));
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        foreach (var (name, value) in environment)
        {
            if (value is null)
            {
                startInfo.Environment.Remove(name);
            }
            else
            {
                startInfo.Environment[name] = value;
            }
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync(ct);
        var stderr = process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return new ContentToolRun(process.ExitCode, await stdout, await stderr);
    }

    /// <summary>
    /// The repository's real content folder, found by walking up to the solution file.
    /// PREPROUND_TEST_CONTENT_DIR overrides it: content waves edit content/ while the suite runs,
    /// so the main session points the suite at a frozen copy instead of a half-written file.
    /// </summary>
    public static string RealContentDirectory()
    {
        var overridden = Environment.GetEnvironmentVariable("PREPROUND_TEST_CONTENT_DIR");
        if (!string.IsNullOrWhiteSpace(overridden))
        {
            return Path.GetFullPath(overridden);
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Prepround.slnx")))
            {
                return Path.GetFullPath(Path.Combine(directory.FullName, "..", "content"));
            }
        }

        throw new InvalidOperationException("Prepround.slnx not found above the test output directory.");
    }

    /// <summary>
    /// Number of questions in the real content, counted from the raw <c>  - id:</c> lines of the topic files rather than
    /// through the reader, so a reader that silently drops questions still fails the import assertions. Content cards
    /// add questions all the time (PRP-018 and later); a literal count here broke on each of them.
    /// </summary>
    public static int RealQuestionCount() =>
        RealTopicFiles().Sum(path => File.ReadLines(path).Count(line => line.StartsWith("  - id: ", StringComparison.Ordinal)));

    /// <summary>
    /// Ids of the real topics marked <c>draft: true</c> (PRP-023), from the raw top-level line rather than the reader,
    /// so a reader that drops the flag still fails the import and catalog assertions.
    /// </summary>
    public static IReadOnlySet<string> RealDraftTopicIds() =>
        RealTopicFiles()
            .Where(IsDraftFile)
            .Select(path => Path.GetFileNameWithoutExtension(path))
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Questions in draft topics, counted from raw lines like <see cref="RealQuestionCount"/>.</summary>
    public static int RealDraftQuestionCount() =>
        RealTopicFiles()
            .Where(IsDraftFile)
            .Sum(path => File.ReadLines(path).Count(line => line.StartsWith("  - id: ", StringComparison.Ordinal)));

    /// <summary>What a default import (without --include-drafts) puts into the database.</summary>
    public static int RealPublishedQuestionCount() => RealQuestionCount() - RealDraftQuestionCount();

    /// <summary>
    /// Question translations an import of the real content stores (PRP-026): see <see cref="QuestionTranslationCount"/>.
    /// Translation waves keep translating topics and then lifting their draft_en, so a literal broke on each topic.
    /// </summary>
    public static int RealQuestionTranslationCount(bool includeDrafts) =>
        QuestionTranslationCount(RealContentDirectory(), includeDrafts);

    /// <summary>
    /// Rows of QuestionTranslations an import of <paramref name="path"/> (a content tree or one topic file) stores:
    /// one per question and language with the question's own text, the explanation and every option's text in that
    /// language (ADR 006). Without --include-drafts a <c>draft: true</c> topic brings none, and a topic with
    /// <c>draft_L: true</c> brings none in L. Counted from the raw lines like <see cref="RealQuestionCount"/>, so a reader
    /// that drops a translation or a draft flag still fails the import assertions.
    /// </summary>
    public static int QuestionTranslationCount(string path, bool includeDrafts) =>
        TopicFiles(path)
            .Where(file => includeDrafts || !IsDraftFile(file))
            .Sum(file =>
            {
                var lines = File.ReadAllLines(file);
                var questions = QuestionBlocks(lines);
                return ContentLanguages.Translations
                    .Where(language => includeDrafts || !lines.Contains($"draft_{language}: true", StringComparer.Ordinal))
                    .Sum(language => questions.Count(question => IsTranslated(question, language)));
            });

    /// <summary>The lines of each question of a topic file, from its <c>  - id:</c> line up to the next one.</summary>
    private static List<string[]> QuestionBlocks(string[] lines)
    {
        var starts = Enumerable.Range(0, lines.Length)
            .Where(i => lines[i].StartsWith("  - id: ", StringComparison.Ordinal))
            .Append(lines.Length)
            .ToList();
        return starts.Zip(starts.Skip(1), (start, end) => lines[start..end]).ToList();
    }

    /// <summary>
    /// The question's own keys sit at its indent (the "- id:" dash plus two). The items of "options:" start with a dash
    /// one level deeper or, as YAML also allows, at the very indent of "options:" (some topics write them so), and an
    /// option's translation is "text_L:" among its keys or right after its dash. Block scalar lines are deeper still.
    /// </summary>
    private static bool IsTranslated(string[] question, string language)
    {
        var keys = Indent(question[0]) + 2;
        bool IsKey(string line, int indent, string key) =>
            Indent(line) == indent && line.AsSpan(indent).StartsWith($"{key}:", StringComparison.Ordinal);

        var optionsAt = Array.FindIndex(question, line => IsKey(line, keys, "options"));
        if (optionsAt < 0 || optionsAt + 1 >= question.Length)
        {
            return false;
        }

        var dash = Indent(question[optionsAt + 1]);
        var items = question.Skip(optionsAt + 1)
            .Where(line => line.Trim().Length > 0)
            .TakeWhile(line => Indent(line) > dash || (Indent(line) == dash && line.AsSpan(dash).StartsWith("- ", StringComparison.Ordinal)))
            .ToList();
        var options = items.Count(line => Indent(line) == dash);
        var translatedOptions = items.Count(line => IsKey(line, dash, $"- text_{language}") || IsKey(line, dash + 2, $"text_{language}"));

        return options > 0
            && translatedOptions == options
            && question.Any(line => IsKey(line, keys, $"text_{language}"))
            && question.Any(line => IsKey(line, keys, $"explanation_{language}"));
    }

    private static int Indent(string line) => line.Length - line.TrimStart(' ').Length;

    private static IEnumerable<string> RealTopicFiles() => TopicFiles(RealContentDirectory());

    private static IEnumerable<string> TopicFiles(string path) =>
        File.Exists(path)
            ? [path]
            : Directory.GetFiles(path, "*.yaml", SearchOption.AllDirectories)
                .Where(file => Path.GetFileName(Path.GetDirectoryName(file)) == "topics");

    private static bool IsDraftFile(string path) => File.ReadLines(path).Contains("draft: true", StringComparer.Ordinal);

    /// <summary>
    /// The real field as the reader sees it. Tests that work on the real content take question ids and counts from here
    /// instead of literals: content waves keep filling and deepening topics (ADR 004, section 8).
    /// </summary>
    public static FieldDefinition RealField()
    {
        var read = new YamlContentReader().Read(RealContentDirectory());
        Assert.True(read.Errors.Count == 0, string.Join(Environment.NewLine, read.Errors));
        return Assert.Single(read.Fields);
    }

    public static TopicDefinition RealTopic(string topicId) => Assert.Single(RealField().Topics, topic => topic.Id == topicId);

    /// <summary>
    /// A <c>&lt;topic&gt;-NNN</c> id the real content does not use yet, for a question a test adds to a copy of it.
    /// A literal (sql-013) stops being free as soon as the topic grows past it.
    /// </summary>
    public static string FreeQuestionId(string topicId)
    {
        var prefix = topicId + "-";
        var taken = RealField().Topics.SelectMany(topic => topic.Questions)
            .Select(question => question.Id)
            .Where(id => id.StartsWith(prefix, StringComparison.Ordinal))
            .Select(id => int.TryParse(id[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : 0);
        return $"{prefix}{taken.DefaultIfEmpty(0).Max() + 1:000}";
    }
}

internal sealed record ContentToolRun(int ExitCode, string Stdout, string Stderr)
{
    public string Output => Stdout + Stderr;

    public string[] StderrLines => Lines(Stderr);

    public string[] StdoutLines => Lines(Stdout);

    private static string[] Lines(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

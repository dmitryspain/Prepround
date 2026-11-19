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

    private static IEnumerable<string> RealTopicFiles() =>
        Directory.GetFiles(RealContentDirectory(), "*.yaml", SearchOption.AllDirectories)
            .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == "topics");

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

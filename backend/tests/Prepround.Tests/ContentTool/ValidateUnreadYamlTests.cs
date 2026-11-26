using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration.UserSecrets;
using Prepround.Tests.Content;
using static Prepround.Tests.ContentTool.ContentToolProcess;

namespace Prepround.Tests.ContentTool;

/// <summary>
/// PRP-014 through the real process on a copy of the real content: no YAML file in the tree is skipped silently,
/// and such a reader error is not followed by misleading rule 4 errors. Also: validate needs no database.
/// </summary>
public class ValidateUnreadYamlTests
{
    private const string SqlTopic = "it/topics/sql.yaml";
    private const string UnreadMessage = "Файл не буде прочитано";
    private const string SqlTopicMissing = "Теми «sql» немає";

    private const string ExtraTopicYaml = """
        id: extra
        title: Додаткова тема
        description: Тема, якої немає в жодному стеку.
        draft_en: true
        questions:
          - id: extra-001
            level: junior
            type: single
            text: Питання?
            options:
              - text: Так
                correct: true
              - text: Ні
            explanation: Бо так.
        """;

    [Theory]
    [InlineData("it/topics/sql.yml")]
    [InlineData("it/topics/sql.YAML")]
    [InlineData("it/sql.yaml")]
    [InlineData("it/topics/extra/sql.yaml")]
    [InlineData("sql.yaml")]
    public async Task Misplaced_or_misnamed_topic_file_is_reported_alone_and_exits_with_1(string target)
    {
        using var content = new RealContentCopy();
        content.Move(SqlTopic, target);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Empty(run.Stdout);
        Assert.Contains(run.StderrLines, line => line.StartsWith($"{target}: {UnreadMessage}", StringComparison.Ordinal));
        Assert.DoesNotContain(run.StderrLines, line => line.Contains(SqlTopicMissing, StringComparison.Ordinal));
        Assert.Equal("Помилок у контенті: 1.", run.StderrLines[^1]);
    }

    [Fact]
    public async Task New_topic_yml_outside_every_stack_is_an_error_not_silence()
    {
        using var content = new RealContentCopy();
        content.Write("it/topics/extra.yml", ExtraTopicYaml);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == ContentErrorExitCode, run.Output);
        Assert.Contains(run.StderrLines, line => line.StartsWith($"it/topics/extra.yml: {UnreadMessage}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Same_topic_as_yaml_outside_every_stack_is_valid()
    {
        // Control for the test above: the topic itself is fine, only the extension made it an error.
        using var content = new RealContentCopy();
        var before = await RunAsync("validate", content.Root);
        content.Write("it/topics/extra.yaml", ExtraTopicYaml);

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        var (topicsBefore, questionsBefore, translationsBefore) = TopicAndQuestionCount(before.Stdout);
        // The extra topic has no English (held back by draft_en, rule 12), so the translations suffix (PRP-025,
        // translation waves) stays as it was.
        Assert.Equal((topicsBefore + 1, questionsBefore + 1, translationsBefore), TopicAndQuestionCount(run.Stdout));
    }

    [Fact]
    public async Task Hidden_and_non_yaml_files_at_any_depth_keep_the_real_content_valid()
    {
        var real = await RunAsync("validate", RealContentDirectory());
        using var content = new RealContentCopy();
        foreach (var folder in new[] { "", "it/", "it/topics/" })
        {
            content.Write($"{folder}.DS_Store", "\0binary");
            content.Write($"{folder}README.md", "# Нотатки автора");
        }

        var run = await RunAsync("validate", content.Root);

        Assert.True(run.ExitCode == SuccessExitCode, run.Output);
        Assert.Equal(real.Stdout, run.Stdout);
        Assert.Empty(run.Stderr);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Host=127.0.0.1;Port=1;Database=prepround;Username=prepround;Password=prepround;Timeout=1")]
    public async Task Validate_neither_needs_nor_opens_the_database(string? connectionString)
    {
        // A private HOME makes the run independent of the machine's user-secrets (ContentTool reads them).
        var home = Directory.CreateTempSubdirectory("prepround-home-");
        try
        {
            if (connectionString is not null)
            {
                var secrets = Path.Combine(home.FullName, ".microsoft", "usersecrets", ContentToolUserSecretsId(), "secrets.json");
                Directory.CreateDirectory(Path.GetDirectoryName(secrets)!);
                await File.WriteAllTextAsync(secrets, $$"""{ "ConnectionStrings": { "Prepround": "{{connectionString}}" } }""",
                    TestContext.Current.CancellationToken);
            }

            using var content = new ContentFixture();

            var run = await RunWithEnvironmentAsync(
                new Dictionary<string, string?> { ["HOME"] = home.FullName, ["APPDATA"] = null },
                "validate", content.Root);

            Assert.True(run.ExitCode == SuccessExitCode, run.Output);
            Assert.Equal([ContentFixture.Summary], run.StdoutLines);
        }
        finally
        {
            home.Delete(recursive: true);
        }
    }

    private static (int Topics, int Questions, string Translations) TopicAndQuestionCount(string stdout)
    {
        var match = Regex.Match(stdout,
            @"^it: .*тем (\d+), питань (\d+)(?:, з них у чернетках [1-9]\d*)?((?:, перекладів [a-z]+: [1-9]\d*)*)$", RegexOptions.Multiline);
        Assert.True(match.Success, stdout);
        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), match.Groups[3].Value);
    }

    private static string ContentToolUserSecretsId() =>
        Assembly.Load("Prepround.ContentTool").GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;
}

/// <summary>A copy of the repository's content in a temporary directory, so a test can break it safely.</summary>
internal sealed class RealContentCopy : IDisposable
{
    public RealContentCopy()
    {
        Root = Directory.CreateTempSubdirectory("prepround-real-content-").FullName;
        var source = RealContentDirectory();
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(Root, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    public string Root { get; }

    public string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));

    public void Write(string relative, string text)
    {
        var path = Path.Combine(Root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    /// <summary>Replaces the first occurrence and fails loudly if the real content no longer contains it.</summary>
    public void Replace(string relative, string oldText, string newText)
    {
        var text = Read(relative);
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        Assert.True(index >= 0, $"{relative} has no \"{oldText}\".");
        Write(relative, string.Concat(text.AsSpan(0, index), newText, text.AsSpan(index + oldText.Length)));
    }

    /// <summary>Two steps, so a case-only rename (sql.yaml → sql.YAML) also works on a case-insensitive disk.</summary>
    public void Move(string fromRelative, string toRelative)
    {
        var from = Path.Combine(Root, fromRelative);
        var to = Path.Combine(Root, toRelative);
        var temporary = Path.Combine(Root, $"move-{Guid.NewGuid():N}.tmp");
        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
        File.Move(from, temporary);
        File.Move(temporary, to);
        Assert.Equal(Path.GetFileName(to), Path.GetFileName(Directory.GetFiles(Path.GetDirectoryName(to)!, Path.GetFileName(to)).Single()));
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
            // A leftover temp directory is not worth failing a test over.
        }
    }
}

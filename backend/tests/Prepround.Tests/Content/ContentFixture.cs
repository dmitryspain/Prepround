namespace Prepround.Tests.Content;

/// <summary>
/// A small valid content tree in a temporary directory: field "it" with three levels, one stack, two topics,
/// two questions. Tests break one thing in it and check that exactly that thing is reported.
/// </summary>
internal sealed class ContentFixture : IDisposable
{
    public const string FieldFile = "it/field.yaml";
    public const string AlphaFile = "it/topics/alpha.yaml";
    public const string BetaFile = "it/topics/beta.yaml";

    /// <summary>What <c>validate</c> prints for the untouched fixture.</summary>
    public const string Summary = "it: рівнів 3, напрямів 1, стеків 1, тем 2, питань 2";

    public const string FieldYaml = """
        id: it
        title: IT
        levels:
          - id: junior
            title: Junior
          - id: middle
            title: Middle
          - id: senior
            title: Senior
        directions:
          - id: backend
            title: Backend
            stacks:
              - id: dotnet
                title: .NET (C#)
                topics: [alpha, beta]
        """;

    // Line numbers matter: tests assert "рядку 11" for the first option's "correct".
    public const string AlphaYaml = """
        id: alpha
        title: Alpha
        description: Перша тема.
        questions:
          - id: alpha-001
            level: junior
            type: single
            text: Питання з одним варіантом?
            options:
              - text: Так
                correct: true
              - text: Ні
            explanation: Бо так.
          - id: alpha-002
            level: senior
            type: multiple
            text: Які варіанти правильні?
            code:
              lang: csharp
              source: |
                if (ready)
                    Run();
            options:
              - text: Перший
                correct: true
              - text: Другий
                correct: true
              - text: Третій
            explanation: Перші два.
        """;

    public const string BetaYaml = """
        id: beta
        title: Beta
        questions: []
        """;

    public ContentFixture()
    {
        Root = Directory.CreateTempSubdirectory("prepround-content-").FullName;
        Write(FieldFile, FieldYaml);
        Write(AlphaFile, AlphaYaml);
        Write(BetaFile, BetaYaml);
    }

    public string Root { get; }

    public string PathOf(string relative) => Path.Combine(Root, relative);

    public string Read(string relative) => File.ReadAllText(PathOf(relative));

    public void Write(string relative, string text)
    {
        var path = PathOf(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    /// <summary>Replaces the first occurrence and fails loudly if the fixture no longer contains it.</summary>
    public void Replace(string relative, string oldText, string newText)
    {
        var text = Read(relative);
        var index = text.IndexOf(oldText, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Fixture {relative} has no \"{oldText}\".");
        Write(relative, string.Concat(text.AsSpan(0, index), newText, text.AsSpan(index + oldText.Length)));
    }

    public void Move(string fromRelative, string toRelative)
    {
        var to = PathOf(toRelative);
        Directory.CreateDirectory(Path.GetDirectoryName(to)!);
        if (Directory.Exists(PathOf(fromRelative)))
        {
            Directory.Move(PathOf(fromRelative), to);
        }
        else
        {
            File.Move(PathOf(fromRelative), to);
        }
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

using Prepround.Application.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// The one "read and check" step shared by validate and import (PRP-014): rules 1–8 run only on content
/// the reader read without errors, so a reader error is never buried under follow-up rule errors.
/// </summary>
public class ContentLoaderTests
{
    private const string FieldFile = "it/field.yaml";

    private static readonly ContentError ReaderError =
        new("it/topics/gamma.yml", string.Empty, "Файл не буде прочитано.");

    [Fact]
    public void Reader_error_is_returned_alone_without_rule_errors()
    {
        var fields = FieldsWithStackTopicMissing();
        var reader = new FakeContentReader(new ContentReadResult(fields, [ReaderError]));

        var result = new ContentLoader(reader, new ContentValidator()).Load("content-root");

        Assert.Equal([ReaderError], result.Errors);
        Assert.Same(fields, result.Fields);
        Assert.Equal(["content-root"], reader.ReadRoots);
    }

    [Fact]
    public void Without_reader_errors_rules_1_to_8_are_checked()
    {
        var fields = FieldsWithStackTopicMissing();
        var reader = new FakeContentReader(new ContentReadResult(fields, []));

        var result = new ContentLoader(reader, new ContentValidator()).Load("content-root");

        var error = Assert.Single(result.Errors);
        Assert.Equal((FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("Теми «gamma» немає", error.Message);
        Assert.Same(fields, result.Fields);
    }

    [Fact]
    public void Content_without_problems_loads_without_errors()
    {
        var fields = Fields(stackTopics: ["alpha"]);
        var reader = new FakeContentReader(new ContentReadResult(fields, []));

        var result = new ContentLoader(reader, new ContentValidator()).Load("content-root");

        Assert.Empty(result.Errors);
        Assert.Same(fields, result.Fields);
    }

    /// <summary>Valid except for rule 4: the stack lists topic "gamma", which has no file.</summary>
    private static IReadOnlyList<FieldDefinition> FieldsWithStackTopicMissing() => Fields(stackTopics: ["alpha", "gamma"]);

    private static IReadOnlyList<FieldDefinition> Fields(IReadOnlyList<string> stackTopics)
    {
        var question = new QuestionDefinition(
            "alpha-001", "junior", "single", "Питання?", null,
            [new OptionDefinition("Так", true), new OptionDefinition("Ні", false)],
            "Бо так.", null);
        // Ukrainian only, so English is held back (rule 12).
        var topic = new TopicDefinition("it/topics/alpha.yaml", "alpha", "Alpha", "Тема.", [question])
        {
            DraftLanguages = new HashSet<string> { "en" },
        };
        var stack = new StackDefinition("dotnet", ".NET", stackTopics);

        return
        [
            new FieldDefinition(FieldFile, "it", "IT",
                [new LevelDefinition("junior", "Junior")],
                [new DirectionDefinition("backend", "Backend", [stack])],
                [topic]),
        ];
    }
}

/// <summary>Returns a canned read result and records which roots were read.</summary>
internal sealed class FakeContentReader(ContentReadResult result) : IContentReader
{
    public List<string> ReadRoots { get; } = [];

    public ContentReadResult Read(string rootDirectory)
    {
        ReadRoots.Add(rootDirectory);
        return result;
    }
}

using Prepround.Application.Content;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// The reader turns YAML into the content model. Structural problems (syntax, unknown keys, wrong shapes)
/// become <see cref="ContentError"/>s with the file and line instead of exceptions (ADR 002, rule 9).
/// </summary>
public class YamlContentReaderTests
{
    private readonly YamlContentReader _reader = new();

    [Fact]
    public void Reads_the_fixture_without_errors_and_keeps_file_order()
    {
        using var content = new ContentFixture();

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        var field = Assert.Single(result.Fields);
        Assert.Equal(ContentFixture.FieldFile, field.File);
        Assert.Equal(("it", "IT"), (field.Id, field.Title));
        Assert.Equal(["junior", "middle", "senior"], field.Levels.Select(level => level.Id));
        var stack = Assert.Single(Assert.Single(field.Directions).Stacks);
        Assert.Equal(["alpha", "beta"], stack.TopicIds);
        Assert.Equal([ContentFixture.AlphaFile, ContentFixture.BetaFile], field.Topics.Select(topic => topic.File));
    }

    [Fact]
    public void Keeps_options_in_file_order_with_their_correct_flags()
    {
        using var content = new ContentFixture();

        var alpha = ReadTopic(content, "alpha");

        var multiple = alpha.Questions[1];
        Assert.Equal("multiple", multiple.Type);
        Assert.Equal(
            [new OptionDefinition("Перший", true), new OptionDefinition("Другий", true), new OptionDefinition("Третій", false)],
            multiple.Options);
    }

    [Fact]
    public void Keeps_code_indentation_and_drops_only_the_trailing_newline()
    {
        using var content = new ContentFixture();

        var alpha = ReadTopic(content, "alpha");

        Assert.Null(alpha.Questions[0].Code);
        Assert.Equal(new CodeBlock("csharp", "if (ready)\n    Run();"), alpha.Questions[1].Code);
    }

    [Fact]
    public void Folded_multiline_text_becomes_one_trimmed_paragraph()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "explanation: Бо так.", "explanation: >\n      Перше речення.\n      Друге речення.\n");

        var alpha = ReadTopic(content, "alpha");

        Assert.Equal("Перше речення. Друге речення.", alpha.Questions[0].Explanation);
    }

    [Fact]
    public void Question_without_sources_key_reads_sources_as_null()
    {
        using var content = new ContentFixture();

        var alpha = ReadTopic(content, "alpha");

        Assert.All(alpha.Questions, question => Assert.Null(question.Sources));
    }

    [Fact]
    public void Sources_are_read_in_file_order_from_block_and_flow_lists()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "    level: junior", """
                level: junior
                sources:
                  - https://www.postgresql.org/docs/18/ddl-constraints.html#DDL-CONSTRAINTS-PRIMARY-KEYS
                  - "https://www.postgresql.org/docs/18/sql-select.html"
            """);
        content.Replace(ContentFixture.AlphaFile, "    level: senior", "    level: senior\n    sources: [https://learn.microsoft.com/en-us/dotnet/]");

        var alpha = ReadTopic(content, "alpha");

        Assert.Equal(
            ["https://www.postgresql.org/docs/18/ddl-constraints.html#DDL-CONSTRAINTS-PRIMARY-KEYS", "https://www.postgresql.org/docs/18/sql-select.html"],
            alpha.Questions[0].Sources);
        Assert.Equal(["https://learn.microsoft.com/en-us/dotnet/"], alpha.Questions[1].Sources);
    }

    [Theory]
    [InlineData("sources: []")]
    [InlineData("sources:")]
    [InlineData("sources: ~")]
    public void Present_but_empty_sources_key_reads_as_an_empty_list_not_null(string line)
    {
        // Null would mean "no key", which is valid; the author who wrote the key meant to give a source (rule 10).
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "    level: junior", $"    level: junior\n    {line}");

        var sources = ReadTopic(content, "alpha").Questions[0].Sources;

        Assert.NotNull(sources);
        Assert.Empty(sources);
    }

    [Theory]
    [InlineData("sources: https://www.postgresql.org/docs/18/", "Значення ключа «sources» у рядку 7 має бути списком.")]
    [InlineData("sources: {url: https://www.postgresql.org/docs/18/}", "Значення ключа «sources» у рядку 7 має бути списком.")]
    [InlineData("sources:\n      - [https://www.postgresql.org/docs/18/]", "Елемент списку «sources» у рядку 8 має бути текстом.")]
    [InlineData("sources:\n      - url: https://www.postgresql.org/docs/18/", "Елемент списку «sources» у рядку 8 має бути текстом.")]
    public void Sources_of_the_wrong_shape_is_a_reader_error_with_the_line(string yaml, string message)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "    level: junior", $"    level: junior\n    {yaml}");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal(new ContentError(ContentFixture.AlphaFile, "alpha-001", message), error);
    }

    [Fact]
    public void Topic_with_empty_question_list_has_no_questions()
    {
        using var content = new ContentFixture();

        Assert.Empty(ReadTopic(content, "beta").Questions);
    }

    [Theory]
    [InlineData(ContentFixture.FieldFile, "title: IT", "titel", "it")]
    [InlineData(ContentFixture.FieldFile, "    title: Junior", "rank", "junior")]
    [InlineData(ContentFixture.FieldFile, "    title: Backend", "order", "backend")]
    [InlineData(ContentFixture.FieldFile, "        title: .NET (C#)", "topic", "dotnet")]
    [InlineData(ContentFixture.AlphaFile, "title: Alpha", "tags", "alpha")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "explaination", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "      lang: csharp", "language", "alpha-002")]
    [InlineData(ContentFixture.AlphaFile, "        correct: true", "corect", "alpha-001")]
    // PRP-023: draft belongs to the topic only; on a question or in field.yaml it stays an unknown key.
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "draft", "alpha-001")]
    [InlineData(ContentFixture.FieldFile, "title: IT", "draft", "it")]
    public void Unknown_key_at_any_depth_is_an_error_with_the_file_and_the_owner_id(
        string file, string afterLine, string unknownKey, string ownerId)
    {
        using var content = new ContentFixture();
        var indent = afterLine[..(afterLine.Length - afterLine.TrimStart().Length)];
        content.Replace(file, afterLine, $"{afterLine}\n{indent}{unknownKey}: x");

        var result = _reader.Read(content.Root);

        var error = Assert.Single(result.Errors);
        Assert.Equal((file, ownerId), (error.File, error.Id));
        Assert.Contains($"«{unknownKey}»", error.Message);
    }

    [Fact]
    public void Typo_corect_reports_the_line_of_the_key()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "correct: true", "corect: true");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal(new ContentError(ContentFixture.AlphaFile, "alpha-001",
            "Невідомий ключ «corect» у рядку 11. Допустимі ключі: text, correct."), error);
    }

    [Theory]
    [InlineData("  - text: Ні", "  - text: [Ні", "12")] // unclosed flow sequence: YamlDotNet throws InvalidOperationException
    [InlineData("  - text: Ні", "  - text: {Ні", "12")] // unclosed flow mapping
    [InlineData("  - text: Ні", "  - text: \"Ні", "12")] // unclosed quote
    [InlineData("    type: single", "     type: single", "7")] // broken indentation
    [InlineData("    type: single", "    type: single\n    type: multiple", "8")] // duplicate key
    public void Yaml_syntax_error_becomes_an_error_with_the_file_and_line_instead_of_an_exception(
        string oldText, string newText, string line)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, oldText, newText);

        var result = _reader.Read(content.Root);

        var error = Assert.Single(result.Errors);
        Assert.Equal((ContentFixture.AlphaFile, "alpha"), (error.File, error.Id));
        Assert.StartsWith("Синтаксична помилка YAML", error.Message);
        Assert.Matches($@"рядк[уа] {line}\b", error.Message);
    }

    [Theory]
    [InlineData("yes")]
    [InlineData("1")]
    [InlineData("правда")]
    public void Correct_accepts_only_true_or_false(string value)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "correct: true", $"correct: {value}");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((ContentFixture.AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("true або false", error.Message);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("draft: true", true)]
    [InlineData("draft: false", false)]
    [InlineData("draft:", false)]
    public void Draft_is_read_from_the_topic_and_absent_or_false_is_not_a_draft(string? line, bool expected)
    {
        // PRP-023: a top-level topic key; it marks only its own topic.
        using var content = new ContentFixture();
        if (line is not null)
        {
            content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", $"description: Перша тема.\n{line}");
        }

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        var topics = Assert.Single(result.Fields).Topics.ToDictionary(topic => topic.Id);
        Assert.Equal(expected, topics["alpha"].IsDraft);
        Assert.False(topics["beta"].IsDraft);
        Assert.Equal(2, topics["alpha"].Questions.Count);
    }

    [Theory]
    [InlineData("maybe", "рядку 4 має бути true або false")]
    [InlineData("yes", "рядку 4 має бути true або false")]
    [InlineData("1", "рядку 4 має бути true або false")]
    [InlineData("[true]", "рядку 4 має бути текстом")]
    public void Draft_accepts_only_true_or_false_and_reports_the_topic_file_and_line(string value, string message)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", $"description: Перша тема.\ndraft: {value}");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((ContentFixture.AlphaFile, "alpha"), (error.File, error.Id));
        Assert.Contains("«draft»", error.Message);
        Assert.Contains(message, error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("# лише коментар\n")]
    public void Empty_topic_file_is_an_error(string text)
    {
        using var content = new ContentFixture();
        content.Write(ContentFixture.BetaFile, text);

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((ContentFixture.BetaFile, "beta"), (error.File, error.Id));
    }

    [Theory]
    [InlineData("- id: beta\n- title: Beta\n")]
    [InlineData("просто текст\n")]
    public void Topic_file_that_is_not_a_mapping_is_an_error(string text)
    {
        using var content = new ContentFixture();
        content.Write(ContentFixture.BetaFile, text);

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal(ContentFixture.BetaFile, error.File);
    }

    [Theory]
    [InlineData("questions: []", "questions: abc", "beta")] // list expected
    [InlineData("id: beta", "id: [beta]", "beta")] // text expected
    public void Value_of_the_wrong_shape_is_an_error_not_an_exception(string oldText, string newText, string id)
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.BetaFile, oldText, newText);

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((ContentFixture.BetaFile, id), (error.File, error.Id));
    }

    [Fact]
    public void Errors_in_several_files_are_all_reported_in_one_run()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "correct: true", "corect: true");
        content.Replace(ContentFixture.BetaFile, "questions: []", "questions: [");

        var errors = _reader.Read(content.Root).Errors;

        Assert.Equal([ContentFixture.AlphaFile, ContentFixture.BetaFile], errors.Select(error => error.File).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Field_folder_without_field_yaml_is_an_error()
    {
        // A non-YAML name, so the only problem left is the missing field.yaml (a .yml copy is an error of its own, PRP-014).
        using var content = new ContentFixture();
        content.Move(ContentFixture.FieldFile, "it/field.txt");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal(ContentFixture.FieldFile, error.File);
    }

    [Fact]
    public void Field_yml_instead_of_field_yaml_reports_both_the_missing_file_and_the_unread_one()
    {
        using var content = new ContentFixture();
        content.Move(ContentFixture.FieldFile, "it/field.yml");

        var errors = _reader.Read(content.Root).Errors;

        Assert.Equal([ContentFixture.FieldFile, "it/field.yml"], errors.Select(error => error.File).Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("it/topics/gamma.yml")]
    [InlineData("it/topics/gamma.YAML")]
    [InlineData("it/topics/gamma.Yml")]
    [InlineData("it/gamma.yaml")]
    [InlineData("it/topics/extra/gamma.yaml")]
    [InlineData("gamma.yaml")]
    [InlineData("gamma.YML")]
    public void Yaml_file_outside_the_read_layout_is_an_error_with_its_relative_path(string file)
    {
        using var content = new ContentFixture();
        content.Write(file, "id: gamma\ntitle: Gamma\nquestions: []\n");

        var result = _reader.Read(content.Root);

        var error = Assert.Single(result.Errors);
        Assert.Equal((file, string.Empty), (error.File, error.Id));
        Assert.Contains("<галузь>/field.yaml", error.Message);
        Assert.Contains("<галузь>/topics/<id>.yaml", error.Message);
        Assert.StartsWith($"{file}: ", error.ToString());
        Assert.Equal(["alpha", "beta"], Assert.Single(result.Fields).Topics.Select(topic => topic.Id));
    }

    [Fact]
    public void Non_yaml_files_at_any_depth_are_skipped()
    {
        using var content = new ContentFixture();
        foreach (var folder in new[] { "", "it/", "it/topics/" })
        {
            content.Write($"{folder}README.md", "# Нотатки");
            content.Write($"{folder}notes.txt", "чернетка: [");
        }

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        Assert.Equal(2, Assert.Single(result.Fields).Topics.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_or_empty_topics_folder_adds_no_reader_error(bool keepEmptyFolder)
    {
        // As before PRP-014: the reader is silent and rule 4 of the validator reports the stack topics.
        using var content = new ContentFixture();
        Directory.Delete(content.PathOf("it/topics"), recursive: true);
        if (keepEmptyFolder)
        {
            Directory.CreateDirectory(content.PathOf("it/topics"));
        }

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        Assert.Empty(Assert.Single(result.Fields).Topics);
    }

    [Fact]
    public void Root_without_field_folders_is_an_error()
    {
        var empty = Directory.CreateTempSubdirectory("prepround-empty-");
        try
        {
            var result = _reader.Read(empty.FullName);

            Assert.Empty(result.Fields);
            Assert.Single(result.Errors);
        }
        finally
        {
            empty.Delete(recursive: true);
        }
    }

    [Fact]
    public void Hidden_folders_and_files_are_skipped()
    {
        using var content = new ContentFixture();
        content.Write(".git/config", "not yaml: [");
        content.Write("it/topics/.draft.yaml", "not yaml: [");
        content.Write(".DS_Store", "binary");

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        Assert.Equal(2, Assert.Single(result.Fields).Topics.Count);
    }

    private TopicDefinition ReadTopic(ContentFixture content, string id)
    {
        var result = _reader.Read(content.Root);
        Assert.Empty(result.Errors);
        return Assert.Single(Assert.Single(result.Fields).Topics, topic => topic.Id == id);
    }
}

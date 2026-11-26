using Prepround.Application.Content;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// PRP-025, ADR 006: the reader takes <c>key_en</c> next to each translated key and keeps every translation apart,
/// per field and per language, so rule 11 can name what is missing. Any other suffix, case or place is rule 9.
/// </summary>
public class YamlContentReaderTranslationTests
{
    private readonly YamlContentReader _reader = new();

    [Fact]
    public void Reads_every_translation_key_into_its_own_value_and_keeps_the_base_texts_and_correct_flags()
    {
        using var content = new ContentFixture();
        TranslatedFixture.TranslateField(content);
        TranslatedFixture.TranslateAlphaAndItsFirstQuestion(content);

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        var field = Assert.Single(result.Fields);
        Assert.Equal(("IT", En(TranslatedFixture.FieldTitle)), (field.Title, field.TitleTranslations));
        Assert.Equal(En(TranslatedFixture.LevelTitle), field.Levels[0].TitleTranslations);
        Assert.Equal([TranslatedTexts.None, TranslatedTexts.None], field.Levels.Skip(1).Select(level => level.TitleTranslations));
        var direction = Assert.Single(field.Directions);
        Assert.Equal(En(TranslatedFixture.DirectionTitle), direction.TitleTranslations);
        Assert.Equal(En(TranslatedFixture.StackTitle), Assert.Single(direction.Stacks).TitleTranslations);

        var alpha = field.Topics[0];
        Assert.Equal(("Alpha", "Перша тема.", false), (alpha.Title, alpha.Description, alpha.IsDraft));
        Assert.Equal(En(TranslatedFixture.TopicTitle), alpha.TitleTranslations);
        Assert.Equal(En(TranslatedFixture.TopicDescription), alpha.DescriptionTranslations);
        Assert.Equal(["en"], alpha.DraftLanguages);

        var question = alpha.Questions[0];
        Assert.Equal(("Питання з одним варіантом?", "Бо так."), (question.Text, question.Explanation));
        Assert.Equal(En(TranslatedFixture.QuestionText), question.TextTranslations);
        Assert.Equal(En(TranslatedFixture.Explanation), question.ExplanationTranslations);
        Assert.Equal(
            [("Так", true, En(TranslatedFixture.FirstOption)), ("Ні", false, En(TranslatedFixture.SecondOption))],
            question.Options.Select(option => (option.Text, option.IsCorrect, option.TextTranslations)));
        Assert.True(question.IsTranslatedTo("en"));
        Assert.False(alpha.Questions[1].IsTranslatedTo("en"));
    }

    [Fact]
    public void Content_without_translation_keys_reads_no_translations_and_no_draft_languages()
    {
        using var content = new ContentFixture();
        content.PublishInEnglish(ContentFixture.AlphaFile);
        content.PublishInEnglish(ContentFixture.BetaFile);

        var field = Assert.Single(_reader.Read(content.Root).Fields);

        Assert.Empty(field.TitleTranslations);
        Assert.All(field.Levels, level => Assert.Empty(level.TitleTranslations));
        Assert.All(field.Topics, topic =>
        {
            Assert.Empty(topic.TitleTranslations);
            Assert.Empty(topic.DescriptionTranslations);
            Assert.Empty(topic.DraftLanguages);
        });
        var questions = field.Topics.SelectMany(topic => topic.Questions).ToList();
        Assert.All(questions, question =>
        {
            Assert.Empty(question.TextTranslations);
            Assert.Empty(question.ExplanationTranslations);
            Assert.All(question.Options, option => Assert.Empty(option.TextTranslations));
            Assert.False(question.IsTranslatedTo("en"));
        });
    }

    [Theory]
    [InlineData("draft_en: true", new[] { "en" })]
    [InlineData("draft_en: false", new string[0])]
    [InlineData("draft_en:", new string[0])]
    public void Draft_en_is_read_like_draft_and_does_not_make_the_topic_a_draft(string line, string[] expected)
    {
        // ADR 006: draft_en holds back only the translation, the base questions stay published.
        using var content = new ContentFixture();
        content.PublishInEnglish(ContentFixture.AlphaFile);
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", $"description: Перша тема.\n{line}");

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        var alpha = Assert.Single(result.Fields).Topics[0];
        Assert.Equal(expected, alpha.DraftLanguages);
        Assert.False(alpha.IsDraft);
        Assert.Equal(2, alpha.Questions.Count);
    }

    [Theory]
    [InlineData("так", "рядку 4 має бути true або false")]
    [InlineData("yes", "рядку 4 має бути true або false")]
    [InlineData("[true]", "рядку 4 має бути текстом")]
    public void Draft_en_accepts_only_true_or_false_like_draft(string value, string message)
    {
        using var content = new ContentFixture();
        content.PublishInEnglish(ContentFixture.AlphaFile);
        content.Replace(ContentFixture.AlphaFile, "description: Перша тема.", $"description: Перша тема.\ndraft_en: {value}");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((ContentFixture.AlphaFile, "alpha"), (error.File, error.Id));
        Assert.Contains("«draft_en»", error.Message);
        Assert.Contains(message, error.Message);
    }

    [Theory]
    [InlineData("text_en: ''")]
    [InlineData("text_en: \"   \"")]
    [InlineData("text_en:")]
    [InlineData("text_en: ~")]
    public void Present_but_blank_translation_is_read_as_an_empty_text_not_as_a_missing_key(string line)
    {
        // Dropping it would turn "the author left it blank" into "not translated", and a whole blank translation
        // would then pass rule 11 silently.
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "    text: Питання з одним варіантом?", $"    text: Питання з одним варіантом?\n    {line}");

        var result = _reader.Read(content.Root);

        Assert.Empty(result.Errors);
        Assert.Equal(En(string.Empty), Assert.Single(result.Fields).Topics[0].Questions[0].TextTranslations);
    }

    [Theory]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "text_de", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "text_EN", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "title: Alpha", "title_EN", "alpha")]
    [InlineData(ContentFixture.AlphaFile, "title: Alpha", "title_uk", "alpha")] // the base language has no suffix
    [InlineData(ContentFixture.AlphaFile, "title: Alpha", "id_en", "alpha")]
    [InlineData(ContentFixture.AlphaFile, "title: Alpha", "draft_EN", "alpha")]
    [InlineData(ContentFixture.AlphaFile, "      lang: csharp", "source_en", "alpha-002")] // code is shared (ADR 006)
    [InlineData(ContentFixture.AlphaFile, "      lang: csharp", "lang_en", "alpha-002")]
    [InlineData(ContentFixture.AlphaFile, "      lang: csharp", "text_en", "alpha-002")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "code_en", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "level_en", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "sources_en", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "title_en", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "    level: junior", "draft_en", "alpha-001")] // draft_L is a topic key
    [InlineData(ContentFixture.AlphaFile, "        correct: true", "explanation_en", "alpha-001")]
    [InlineData(ContentFixture.AlphaFile, "        correct: true", "correct_en", "alpha-001")]
    [InlineData(ContentFixture.FieldFile, "title: IT", "description_en", "it")]
    [InlineData(ContentFixture.FieldFile, "title: IT", "draft_en", "it")]
    [InlineData(ContentFixture.FieldFile, "    title: Junior", "text_en", "junior")]
    [InlineData(ContentFixture.FieldFile, "        title: .NET (C#)", "topics_en", "dotnet")]
    public void Translation_key_in_another_language_case_or_place_is_an_unknown_key_with_its_name(
        string file, string afterLine, string unknownKey, string ownerId)
    {
        using var content = new ContentFixture();
        var indent = afterLine[..(afterLine.Length - afterLine.TrimStart().Length)];
        content.Replace(file, afterLine, $"{afterLine}\n{indent}{unknownKey}: x");

        var error = Assert.Single(_reader.Read(content.Root).Errors);

        Assert.Equal((file, ownerId), (error.File, error.Id));
        Assert.Contains($"«{unknownKey}»", error.Message);
    }

    [Fact]
    public void Unknown_translation_key_says_how_a_translation_key_is_written_and_shared_keys_list_the_allowed_ones()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "    level: junior", "    level: junior\n    text_de: x");
        content.Replace(ContentFixture.AlphaFile, "      lang: csharp", "      lang: csharp\n      source_en: x");

        var errors = _reader.Read(content.Root).Errors;

        Assert.Equal(
            [
                new ContentError(ContentFixture.AlphaFile, "alpha-001",
                    "Невідомий ключ «text_de» у рядку 7. Ключ перекладу пишеться як text_<мова>, мови перекладу: en."),
                new ContentError(ContentFixture.AlphaFile, "alpha-002",
                    "Невідомий ключ «source_en» у рядку 21. Допустимі ключі: lang, source."),
            ],
            errors);
    }

    private static TranslatedTexts En(string text) => new([new("en", text)]);
}

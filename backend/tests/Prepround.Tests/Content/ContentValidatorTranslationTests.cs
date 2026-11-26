using Prepround.Application.Content;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// Rule 11 (PRP-025, ADR 006): a translated value is never blank, a question is translated into a language
/// completely or not at all, and its translated options do not repeat. Rule 12 (PRP-031): a topic without
/// <c>draft: true</c> and <c>draft_en: true</c> is published in English and has all of it. Each test breaks one thing
/// in a valid, fully translated field whose topic is published in English.
/// </summary>
public class ContentValidatorTranslationTests
{
    private const string FieldFile = "it/field.yaml";
    private const string AlphaFile = "it/topics/alpha.yaml";
    private const string Incomplete = "Неповний переклад мовою en: ";

    private readonly ContentValidator _validator = new();

    [Fact]
    public void Fully_translated_field_has_no_errors()
    {
        Assert.Empty(_validator.Validate([Field()]));
    }

    [Fact]
    public void Untranslated_question_next_to_a_translated_one_has_no_errors_while_the_topic_holds_english_back()
    {
        // Rule 11 takes "none"; only rule 12 asks for more, and draft_en lifts it (the way translation waves work).
        var field = WithQuestion(Field(), Untranslated(Question()));

        Assert.Empty(_validator.Validate([WithAlpha(field, alpha => alpha with { DraftLanguages = new HashSet<string> { "en" } })]));
    }

    [Fact]
    public void Untranslated_question_in_a_topic_published_in_english_is_one_error_with_the_file_id_and_language()
    {
        var field = WithQuestion(Field(), Untranslated(Question()));

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal(new ContentError(AlphaFile, "alpha-001",
            "Тема опублікована мовою en, а перекладу питання цією мовою немає. " +
            "Потрібні text_en, explanation_en і text_en кожного варіанта або позначка теми draft_en: true."), error);
    }

    [Fact]
    public void Every_untranslated_question_of_a_published_topic_is_reported_not_only_the_first()
    {
        var alpha = Field().Topics[0];
        var field = WithAlpha(Field(), _ => alpha with { Questions = alpha.Questions.Select(Untranslated).ToList() });

        var errors = _validator.Validate([field]);

        Assert.Equal(["alpha-001", "alpha-002"], errors.Select(error => error.Id));
        Assert.All(errors, error => Assert.StartsWith("Тема опублікована мовою en, ", error.Message));
    }

    [Theory]
    [InlineData("draft_en")]
    [InlineData("draft")]
    [InlineData("both")]
    public void Draft_or_draft_en_lifts_rule_12_from_a_topic_without_any_english(string flag)
    {
        var alpha = Field().Topics[0] with
        {
            TitleTranslations = TranslatedTexts.None,
            DescriptionTranslations = TranslatedTexts.None,
            Questions = [Untranslated(Question()), Untranslated(Question()) with { Id = "alpha-002" }],
            IsDraft = flag is "draft" or "both",
            DraftLanguages = flag is "draft_en" or "both" ? new HashSet<string> { "en" } : new HashSet<string>(),
        };

        Assert.Empty(_validator.Validate([WithAlpha(Field(), _ => alpha)]));
        // Control: the same topic without either flag breaks rule 12 four times (title, description, two questions).
        Assert.Equal(4, _validator.Validate([WithAlpha(Field(), _ => alpha with { IsDraft = false, DraftLanguages = new HashSet<string>() })]).Count);
    }

    [Fact]
    public void Topic_published_in_english_without_title_en_is_an_error()
    {
        var field = WithAlpha(Field(), alpha => alpha with { TitleTranslations = TranslatedTexts.None });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal(new ContentError(AlphaFile, "alpha",
            "Тема опублікована мовою en, а title_en немає. Перекладіть назву або позначте тему draft_en: true."), error);
    }

    [Fact]
    public void Topic_published_in_english_with_a_description_but_without_description_en_is_an_error()
    {
        var field = WithAlpha(Field(), alpha => alpha with { DescriptionTranslations = TranslatedTexts.None });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal(new ContentError(AlphaFile, "alpha",
            "Тема опублікована мовою en, а description_en немає. Перекладіть опис або позначте тему draft_en: true."), error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Topic_published_in_english_without_a_description_needs_no_description_en(string description)
    {
        var field = WithAlpha(Field(), alpha => alpha with { Description = description, DescriptionTranslations = TranslatedTexts.None });

        Assert.Empty(_validator.Validate([field]));
    }

    [Fact]
    public void Empty_draft_topic_breaks_no_rule_and_an_empty_published_one_still_needs_title_en()
    {
        var empty = Field().Topics[0] with
        {
            TitleTranslations = TranslatedTexts.None,
            DescriptionTranslations = TranslatedTexts.None,
            Questions = [],
        };

        Assert.Empty(_validator.Validate([WithAlpha(Field(), _ => empty with { IsDraft = true })]));
        var errors = _validator.Validate([WithAlpha(Field(), _ => empty)]);
        Assert.Equal(2, errors.Count);
        Assert.Contains("а title_en немає", errors[0].Message);
        Assert.Contains("а description_en немає", errors[1].Message);
    }

    [Fact]
    public void Rule_12_holds_in_every_field_and_names_the_file_of_the_topic()
    {
        var other = new FieldDefinition("med/field.yaml", "med", "Медицина", [new LevelDefinition("phd", "PhD")], [],
            [new TopicDefinition("med/topics/anatomy.yaml", "anatomy", "Анатомія", "", [])]);

        var error = Assert.Single(_validator.Validate([Field(), other]));

        Assert.Equal(("med/topics/anatomy.yaml", "anatomy"), (error.File, error.Id));
        Assert.Contains("title_en", error.Message);
    }

    [Fact]
    public void Draft_en_on_a_topic_without_any_translation_is_valid()
    {
        // The translation of a topic has only just started (card PRP-025, "Для tester").
        var field = Field();
        var alpha = field.Topics[0] with
        {
            TitleTranslations = TranslatedTexts.None,
            DescriptionTranslations = TranslatedTexts.None,
            DraftLanguages = new HashSet<string> { "en" },
            Questions = [Untranslated(Question())],
        };

        Assert.Empty(_validator.Validate([field with { Topics = [alpha] }]));
    }

    [Fact]
    public void Option_translated_into_the_same_text_as_its_base_is_fine()
    {
        // Code literals such as `4` read the same in every language; only a repeat within one language is an error.
        var question = Question() with
        {
            Options = [Option("`4`", true, "`4`"), Option("`3`", false, "`3`")],
        };

        Assert.Empty(_validator.Validate([WithQuestion(Field(), question)]));
    }

    [Theory]
    [InlineData("text", "немає text_en")]
    [InlineData("explanation", "немає explanation_en")]
    [InlineData("option-1", "варіант 1: немає text_en")]
    [InlineData("option-3", "варіант 3: немає text_en")]
    public void Question_missing_one_translated_key_is_one_error_with_the_file_id_language_and_key(string dropped, string missing)
    {
        var question = Question();
        question = dropped switch
        {
            "text" => question with { TextTranslations = TranslatedTexts.None },
            "explanation" => question with { ExplanationTranslations = TranslatedTexts.None },
            _ => question with { Options = WithoutTranslation(question.Options, int.Parse(dropped["option-".Length..]) - 1) },
        };

        var error = Assert.Single(_validator.Validate([WithQuestion(Field(), question)]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.StartsWith(Incomplete, error.Message);
        Assert.Contains(missing, error.Message);
    }

    [Fact]
    public void Question_with_only_its_text_translated_lists_every_missing_key_in_one_error()
    {
        var question = Untranslated(Question()) with { TextTranslations = En("What?") };

        var error = Assert.Single(_validator.Validate([WithQuestion(Field(), question)]));

        Assert.Equal(new ContentError(AlphaFile, "alpha-001",
            "Неповний переклад мовою en: немає explanation_en; варіант 1: немає text_en; варіант 2: немає text_en; " +
            "варіант 3: немає text_en. Потрібні text_en, explanation_en і text_en кожного варіанта або жодного з них."), error);
    }

    [Theory]
    [InlineData("text", "", "Порожній текст питання (text_en).")]
    [InlineData("text", "   ", "Порожній текст питання (text_en).")]
    [InlineData("explanation", "", "Порожнє пояснення (explanation_en).")]
    [InlineData("explanation", " \n ", "Порожнє пояснення (explanation_en).")]
    [InlineData("option-2", "", "Варіант №2 порожній (text_en).")]
    [InlineData("option-2", "  ", "Варіант №2 порожній (text_en).")]
    public void Blank_translated_text_explanation_or_option_is_one_error_and_not_an_incomplete_translation(
        string key, string blank, string message)
    {
        var question = Question();
        question = key switch
        {
            "text" => question with { TextTranslations = En(blank) },
            "explanation" => question with { ExplanationTranslations = En(blank) },
            _ => question with { Options = WithTranslation(question.Options, 1, blank) },
        };

        var error = Assert.Single(_validator.Validate([WithQuestion(Field(), question)]));

        Assert.Equal(new ContentError(AlphaFile, "alpha-001", message), error);
    }

    [Theory]
    [InlineData("Yes", "Yes")]
    [InlineData("Yes", " Yes ")]
    public void Repeated_translated_option_text_in_one_question_is_an_error(string first, string second)
    {
        var question = Question() with
        {
            Options = [Option("Так", true, first), Option("Ні", false, second), Option("Не знаю", false, "Don't know")],
        };

        var error = Assert.Single(_validator.Validate([WithQuestion(Field(), question)]));

        Assert.Equal(new ContentError(AlphaFile, "alpha-001", "Варіант «Yes» повторюється (text_en)."), error);
    }

    [Theory]
    [InlineData("field", FieldFile, "it", "Порожня назва галузі (title_en).")]
    [InlineData("level", FieldFile, "middle", "Порожня назва рівня (title_en).")]
    [InlineData("direction", FieldFile, "backend", "Порожня назва напряму (title_en).")]
    [InlineData("stack", FieldFile, "dotnet", "Порожня назва стеку (title_en).")]
    [InlineData("topic", AlphaFile, "alpha", "Порожня назва теми (title_en).")]
    [InlineData("description", AlphaFile, "alpha", "Порожній опис теми (description_en).")]
    public void Blank_translated_title_or_topic_description_is_an_error(string entity, string file, string id, string message)
    {
        var field = Field();
        var blank = En(" ");
        var direction = field.Directions[0];
        field = entity switch
        {
            "field" => field with { TitleTranslations = blank },
            "level" => field with { Levels = [field.Levels[0], field.Levels[1] with { TitleTranslations = blank }, field.Levels[2]] },
            "direction" => field with { Directions = [direction with { TitleTranslations = blank }] },
            "stack" => field with { Directions = [direction with { Stacks = [direction.Stacks[0] with { TitleTranslations = blank }] }] },
            "topic" => field with { Topics = [field.Topics[0] with { TitleTranslations = blank }] },
            _ => field with { Topics = [field.Topics[0] with { DescriptionTranslations = blank }] },
        };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal(new ContentError(file, id, message), error);
    }

    [Fact]
    public void Rule_11_errors_are_reported_together_with_the_base_rule_errors_of_the_same_question()
    {
        var question = Question() with { Explanation = "", TextTranslations = TranslatedTexts.None };

        var errors = _validator.Validate([WithQuestion(Field(), question)]);

        Assert.Equal(2, errors.Count);
        Assert.Contains(errors, error => error.Message == "Порожнє пояснення (explanation).");
        Assert.Contains(errors, error => error.Message.StartsWith(Incomplete, StringComparison.Ordinal));
    }

    [Fact]
    public void Rule_12_does_not_run_when_the_reader_found_errors()
    {
        // ADR 002, "Уточнення реалізації": a half-read catalog is not checked by the rules, rule 12 included.
        using var content = new ContentFixture();
        content.PublishInEnglish(ContentFixture.AlphaFile);
        content.Replace(ContentFixture.BetaFile, "title: Beta", "title: Beta\ntitel_en: Beta");
        var loader = new ContentLoader(new YamlContentReader(), _validator);

        var error = Assert.Single(loader.Load(content.Root).Errors);
        Assert.Equal((ContentFixture.BetaFile, "beta"), (error.File, error.Id));
        Assert.Contains("«titel_en»", error.Message);

        // Control: without the reader error the same alpha breaks rule 12 (title, description, both questions).
        content.Replace(ContentFixture.BetaFile, "titel_en: Beta", "# titel_en: Beta");
        var ruleErrors = loader.Load(content.Root).Errors;
        Assert.Equal(["alpha", "alpha", "alpha-001", "alpha-002"], ruleErrors.Select(ruleError => ruleError.Id));
        Assert.All(ruleErrors, ruleError => Assert.StartsWith("Тема опублікована мовою en, ", ruleError.Message));
    }

    [Fact]
    public void Rule_11_does_not_run_when_the_reader_found_errors()
    {
        // ADR 002, "Уточнення реалізації": a half-read catalog is not checked by the rules.
        using var content = new ContentFixture();
        TranslatedFixture.TranslateAlphaAndItsFirstQuestion(content);
        content.Replace(ContentFixture.AlphaFile, "    explanation_en: ", "    explanation_ru: ");
        var loader = new ContentLoader(new YamlContentReader(), _validator);

        var error = Assert.Single(loader.Load(content.Root).Errors);
        Assert.Contains("«explanation_ru»", error.Message);

        content.Replace(ContentFixture.AlphaFile, "    explanation_ru: ", "    # explanation_en: ");
        var ruleError = Assert.Single(loader.Load(content.Root).Errors);
        Assert.Equal((ContentFixture.AlphaFile, "alpha-001"), (ruleError.File, ruleError.Id));
        Assert.StartsWith(Incomplete + "немає explanation_en.", ruleError.Message);
    }

    private static TranslatedTexts En(string text) => new([new("en", text)]);

    private static OptionDefinition Option(string text, bool isCorrect, string english) =>
        new(text, isCorrect) { TextTranslations = En(english) };

    private static QuestionDefinition Question() => new(
        "alpha-001", "junior", "single", "Питання?", null,
        [Option("Так", true, "Yes"), Option("Ні", false, "No"), Option("Не знаю", false, "Don't know")],
        "Бо так.", null)
    {
        TextTranslations = En("A question?"),
        ExplanationTranslations = En("Because."),
    };

    private static QuestionDefinition Untranslated(QuestionDefinition question) => question with
    {
        TextTranslations = TranslatedTexts.None,
        ExplanationTranslations = TranslatedTexts.None,
        Options = question.Options.Select(option => option with { TextTranslations = TranslatedTexts.None }).ToList(),
    };

    private static List<OptionDefinition> WithoutTranslation(IReadOnlyList<OptionDefinition> options, int index) =>
        options.Select((option, i) => i == index ? option with { TextTranslations = TranslatedTexts.None } : option).ToList();

    private static List<OptionDefinition> WithTranslation(IReadOnlyList<OptionDefinition> options, int index, string english) =>
        options.Select((option, i) => i == index ? option with { TextTranslations = En(english) } : option).ToList();

    /// <summary>One topic "alpha" with a translated question, plus a second, translated question.</summary>
    private static FieldDefinition Field() => new(
        FieldFile, "it", "IT",
        [
            new LevelDefinition("junior", "Junior") { TitleTranslations = En("Junior") },
            new LevelDefinition("middle", "Middle") { TitleTranslations = En("Middle") },
            new LevelDefinition("senior", "Senior"),
        ],
        [
            new DirectionDefinition("backend", "Backend",
                [new StackDefinition("dotnet", ".NET (C#)", ["alpha"]) { TitleTranslations = En(".NET (C#)") }])
            {
                TitleTranslations = En("Backend"),
            },
        ],
        [
            new TopicDefinition(AlphaFile, "alpha", "Alpha", "Перша тема.",
                [Question(), Question() with { Id = "alpha-002" }])
            {
                TitleTranslations = En("Alpha"),
                DescriptionTranslations = En("The first topic."),
            },
        ])
    {
        TitleTranslations = En("IT"),
    };

    private static FieldDefinition WithAlpha(FieldDefinition field, Func<TopicDefinition, TopicDefinition> change) =>
        field with { Topics = [change(field.Topics[0]), .. field.Topics.Skip(1)] };

    private static FieldDefinition WithQuestion(FieldDefinition field, QuestionDefinition question)
    {
        var alpha = field.Topics[0];
        return field with { Topics = [alpha with { Questions = [question, .. alpha.Questions.Skip(1)] }] };
    }
}

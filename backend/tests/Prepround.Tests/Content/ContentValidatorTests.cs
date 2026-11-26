using Prepround.Application.Content;

namespace Prepround.Tests.Content;

/// <summary>
/// Rules 1–8 of ADR 002 and rule 10 of ADR 004 on an in-memory model: each test breaks one thing in a valid field.
/// The field is in Ukrainian only, so every topic holds English back (rule 12 has tests of its own).
/// </summary>
public class ContentValidatorTests
{
    private const string FieldFile = "it/field.yaml";
    private const string AlphaFile = "it/topics/alpha.yaml";
    private const string BetaFile = "it/topics/beta.yaml";
    private const string PgConstraints = "https://www.postgresql.org/docs/18/ddl-constraints.html";
    private const string PgGroupBy = "https://www.postgresql.org/docs/18/queries-table-expressions.html#QUERIES-GROUP";

    private readonly ContentValidator _validator = new();

    [Fact]
    public void Valid_field_has_no_errors()
    {
        Assert.Empty(_validator.Validate([Field()]));
    }

    [Fact]
    public void Level_added_only_as_data_is_accepted()
    {
        var field = Field() with { Levels = [.. Levels(), new LevelDefinition("architect", "Architect")] };
        field = WithAlphaQuestion(field, 0, Single() with { LevelId = "architect" });

        Assert.Empty(_validator.Validate([field]));
    }

    [Fact]
    public void Field_with_its_own_level_scale_validates_against_it_not_against_another_field()
    {
        var science = new FieldDefinition("science/field.yaml", "science", "Наука",
            [new LevelDefinition("aspirant", "Аспірант"), new LevelDefinition("doctor", "Доктор наук")],
            [],
            [UkrainianOnly(new TopicDefinition("science/topics/physics.yaml", "physics", "Фізика", "", [Single("physics-001") with { LevelId = "doctor" }]))]);

        Assert.Empty(_validator.Validate([Field(), science]));
        var error = Assert.Single(_validator.Validate([Field(), science with
        {
            Topics = [science.Topics[0] with { Questions = [Single("physics-001") with { LevelId = "junior" }] }],
        }]));
        Assert.Equal(("science/topics/physics.yaml", "physics-001"), (error.File, error.Id));
    }

    // Rule 1.
    [Theory]
    [InlineData("Alpha-001")]
    [InlineData("alpha_001")]
    [InlineData("alpha 001")]
    [InlineData("alpha--001")]
    [InlineData("-alpha")]
    [InlineData("alpha-")]
    [InlineData("альфа-001")]
    [InlineData("alpha-001\n")]
    public void Question_id_that_is_not_kebab_case_is_an_error(string id)
    {
        var field = WithAlphaQuestion(Field(), 0, Single(id));

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, id), (error.File, error.Id));
        Assert.Contains("kebab-case", error.Message);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("001")]
    [InlineData("ef-core-012")]
    [InlineData("go2-http")]
    public void Kebab_case_ids_are_accepted(string id)
    {
        Assert.Empty(_validator.Validate([WithAlphaQuestion(Field(), 0, Single(id))]));
    }

    [Fact]
    public void Ids_of_field_level_direction_stack_and_topic_must_be_kebab_case_too()
    {
        var field = Field() with
        {
            File = "IT/field.yaml",
            Id = "IT",
            Levels = [new LevelDefinition("Junior", "Junior"), .. Levels().Skip(1)],
            Directions = [new DirectionDefinition("Back_end", "Backend", [new StackDefinition("Dot.Net", ".NET", ["Alpha", "beta"])])],
        };
        field = WithAlphaQuestion(field, 0, Single() with { LevelId = "Junior" });
        field = field with { Topics = [field.Topics[0] with { File = "IT/topics/Alpha.yaml", Id = "Alpha" }, .. field.Topics.Skip(1)] };

        var errors = _validator.Validate([field]);

        Assert.Equal(["Alpha", "Back_end", "Dot.Net", "IT", "Junior"], errors.Select(error => error.Id).Order(StringComparer.Ordinal));
        Assert.All(errors, error => Assert.Contains("kebab-case", error.Message));
    }

    [Fact]
    public void Missing_question_id_is_reported_by_position()
    {
        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 1, Multiple(""))]));

        Assert.Equal((AlphaFile, "питання №2"), (error.File, error.Id));
    }

    // Rule 2.
    [Fact]
    public void Same_question_id_in_two_files_is_an_error_that_names_the_first_file()
    {
        var field = Field();
        field = field with { Topics = [field.Topics[0], field.Topics[1] with { Questions = [Single("alpha-001")] }] };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((BetaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains(AlphaFile, error.Message);
    }

    [Fact]
    public void Same_question_id_in_two_fields_is_an_error()
    {
        var other = new FieldDefinition("med/field.yaml", "med", "Медицина", [new LevelDefinition("phd", "PhD")], [],
            [UkrainianOnly(new TopicDefinition("med/topics/anatomy.yaml", "anatomy", "Анатомія", "", [Single("alpha-001") with { LevelId = "phd" }]))]);

        var error = Assert.Single(_validator.Validate([Field(), other]));

        Assert.Equal(("med/topics/anatomy.yaml", "alpha-001"), (error.File, error.Id));
    }

    [Fact]
    public void Same_level_id_twice_is_an_error()
    {
        var field = Field() with { Levels = [.. Levels(), new LevelDefinition("middle", "Middle again")] };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((FieldFile, "middle"), (error.File, error.Id));
    }

    [Fact]
    public void Same_direction_id_twice_is_an_error()
    {
        var field = Field();
        field = field with { Directions = [field.Directions[0], new DirectionDefinition("backend", "Backend again", [])] };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((FieldFile, "backend"), (error.File, error.Id));
    }

    [Fact]
    public void Same_stack_id_in_two_directions_is_an_error()
    {
        var field = Field();
        field = field with
        {
            Directions =
            [
                field.Directions[0],
                new DirectionDefinition("frontend", "Frontend", [new StackDefinition("dotnet", "Blazor", ["alpha"])]),
            ],
        };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((FieldFile, "dotnet"), (error.File, error.Id));
    }

    [Fact]
    public void Same_topic_id_in_two_files_is_an_error()
    {
        var field = Field();
        field = field with { Topics = [.. field.Topics, new TopicDefinition("it/topics/alpha.yaml", "alpha", "Alpha copy", "", [])] };

        Assert.Contains(_validator.Validate([field]), error => error.Id == "alpha" && error.Message.Contains("повторюється"));
    }

    // Rule 3.
    [Fact]
    public void Topic_file_name_that_differs_from_its_id_is_an_error()
    {
        var field = Field();
        field = field with { Topics = [field.Topics[0], field.Topics[1] with { File = "it/topics/beta-old.yaml" }] };

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal(("it/topics/beta-old.yaml", "beta"), (error.File, error.Id));
    }

    [Fact]
    public void Field_folder_that_differs_from_its_id_is_an_error()
    {
        var error = Assert.Single(_validator.Validate([Field() with { File = "tech/field.yaml" }]));

        Assert.Equal(("tech/field.yaml", "it"), (error.File, error.Id));
    }

    // Rule 4.
    [Fact]
    public void Stack_topic_that_does_not_exist_is_an_error()
    {
        var field = WithStackTopics(Field(), ["alpha", "beta", "gamma"]);

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("gamma", error.Message);
    }

    [Fact]
    public void Same_topic_twice_in_one_stack_is_an_error()
    {
        var field = WithStackTopics(Field(), ["alpha", "beta", "alpha"]);

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((FieldFile, "dotnet"), (error.File, error.Id));
        Assert.Contains("alpha", error.Message);
    }

    [Fact]
    public void Same_topic_in_two_stacks_is_fine()
    {
        var field = Field();
        field = field with
        {
            Directions =
            [
                field.Directions[0],
                new DirectionDefinition("frontend", "Frontend", [new StackDefinition("react", "React", ["beta", "alpha"])]),
            ],
        };

        Assert.Empty(_validator.Validate([field]));
    }

    // Rule 5.
    [Theory]
    [InlineData("lead")]
    [InlineData("Junior")]
    [InlineData("")]
    public void Question_level_outside_the_field_scale_is_an_error(string level)
    {
        var field = WithAlphaQuestion(Field(), 0, Single() with { LevelId = level });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
    }

    // Rule 6.
    [Theory]
    [InlineData("Single")]
    [InlineData("multi")]
    [InlineData("")]
    public void Unknown_question_type_is_an_error(string type)
    {
        var field = WithAlphaQuestion(Field(), 0, Single() with { Type = type });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void Single_question_needs_exactly_one_correct_option(int correctCount)
    {
        var options = Enumerable.Range(1, 3).Select(i => new OptionDefinition($"Варіант {i}", i <= correctCount)).ToList();
        var field = WithAlphaQuestion(Field(), 0, Single() with { Options = options });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("single", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Multiple_question_needs_at_least_two_correct_options(int correctCount)
    {
        var options = Enumerable.Range(1, 3).Select(i => new OptionDefinition($"Варіант {i}", i <= correctCount)).ToList();
        var field = WithAlphaQuestion(Field(), 1, Multiple() with { Options = options });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-002"), (error.File, error.Id));
        Assert.Contains("multiple", error.Message);
    }

    [Fact]
    public void Multiple_question_needs_at_least_one_wrong_option()
    {
        var options = Enumerable.Range(1, 3).Select(i => new OptionDefinition($"Варіант {i}", true)).ToList();
        var field = WithAlphaQuestion(Field(), 1, Multiple() with { Options = options });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-002"), (error.File, error.Id));
    }

    [Fact]
    public void Multiple_question_with_two_correct_and_one_wrong_is_the_minimum_that_passes()
    {
        var options = new[] { new OptionDefinition("A", true), new OptionDefinition("B", true), new OptionDefinition("C", false) };

        Assert.Empty(_validator.Validate([WithAlphaQuestion(Field(), 1, Multiple() with { Options = options })]));
    }

    // Rule 7.
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9)]
    public void Option_count_outside_two_to_eight_is_an_error(int count)
    {
        var field = WithAlphaQuestion(Field(), 0, Single() with { Options = OptionsWithFirstCorrect(count) });

        var errors = _validator.Validate([field]);

        Assert.Contains(errors, error => error.Id == "alpha-001" && error.Message.Contains($"а їх {count}"));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(8)]
    public void Two_and_eight_options_are_accepted(int count)
    {
        Assert.Empty(_validator.Validate([WithAlphaQuestion(Field(), 0, Single() with { Options = OptionsWithFirstCorrect(count) })]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_option_text_is_an_error(string text)
    {
        var options = new[] { new OptionDefinition("Так", true), new OptionDefinition(text, false) };
        var field = WithAlphaQuestion(Field(), 0, Single() with { Options = options });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
    }

    [Fact]
    public void Repeated_option_text_in_one_question_is_an_error()
    {
        var options = new[] { new OptionDefinition("Так", true), new OptionDefinition("Ні", false), new OptionDefinition("Ні", false) };
        var field = WithAlphaQuestion(Field(), 0, Single() with { Options = options });

        var error = Assert.Single(_validator.Validate([field]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("«Ні»", error.Message);
    }

    // Rule 8.
    [Theory]
    [InlineData("")]
    [InlineData(" \n ")]
    public void Empty_question_text_is_an_error(string text)
    {
        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, Single() with { Text = text })]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("text", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Empty_explanation_is_an_error(string explanation)
    {
        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, Single() with { Explanation = explanation })]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("explanation", error.Message);
    }

    [Fact]
    public void Code_block_with_empty_source_is_an_error()
    {
        var question = Multiple() with { Code = new CodeBlock("csharp", "   ") };

        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 1, question)]));

        Assert.Equal((AlphaFile, "alpha-002"), (error.File, error.Id));
        Assert.Contains("code.source", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("C#")]
    [InlineData("PL/pgSQL")]
    [InlineData("objective c")]
    public void Code_lang_that_is_missing_or_not_kebab_case_is_an_error(string lang)
    {
        var question = Multiple() with { Code = new CodeBlock(lang, "SELECT 1;") };

        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 1, question)]));

        Assert.Equal((AlphaFile, "alpha-002"), (error.File, error.Id));
    }

    // Rule 10 (ADR 004): sources are optional, but when the key is present every item is a real https link.
    [Fact]
    public void Absolute_https_sources_with_anchor_and_query_are_accepted()
    {
        string[] sources = [PgConstraints, PgGroupBy, "https://learn.microsoft.com/en-us/dotnet/api/system.uri?view=net-10.0"];

        Assert.Empty(_validator.Validate([WithAlphaQuestion(Field(), 0, Single() with { Sources = sources })]));
    }

    [Fact]
    public void Same_source_in_two_questions_is_not_a_repeat()
    {
        var field = WithAlphaQuestion(Field(), 0, Single() with { Sources = [PgConstraints] });
        field = WithAlphaQuestion(field, 1, Multiple() with { Sources = [PgConstraints] });

        Assert.Empty(_validator.Validate([field]));
    }

    [Fact]
    public void Present_but_empty_sources_list_is_an_error()
    {
        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, Single() with { Sources = [] })]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("sources", error.Message);
    }

    [Theory]
    [InlineData("http://www.postgresql.org/docs/18/ddl-constraints.html")]
    [InlineData("ftp://ftp.postgresql.org/pub/")]
    [InlineData("file:///etc/hosts")]
    [InlineData("/docs/18/")] // an absolute file URI for Uri on Unix: only the scheme check stops it
    [InlineData("javascript:alert(1)")] // the link will be rendered on /result later (ADR 004, "Ideas")
    [InlineData("mailto:docs@example.com")]
    [InlineData("www.postgresql.org/docs/18/")]
    [InlineData("docs/18/ddl-constraints.html")]
    [InlineData("https://")]
    [InlineData("https://www.postgresql.org/docs/18/ddl constraints.html")] // Uri itself would escape the space
    [InlineData("https://www.postgresql.org/docs/18/\tddl-constraints.html")]
    [InlineData("https://www.postgresql.org/docs/18/ ddl-constraints.html")]
    public void Source_that_is_not_an_absolute_https_url_without_whitespace_is_an_error(string source)
    {
        var question = Single() with { Sources = [PgConstraints, source] };

        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, question)]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains($"«{source}»", error.Message);
        Assert.Contains("https://", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_source_item_is_an_error_with_its_position(string source)
    {
        var question = Single() with { Sources = [PgConstraints, source] };

        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, question)]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains("№2", error.Message);
    }

    [Fact]
    public void Repeated_source_in_one_question_is_an_error()
    {
        var question = Single() with { Sources = [PgConstraints, PgGroupBy, PgConstraints] };

        var error = Assert.Single(_validator.Validate([WithAlphaQuestion(Field(), 0, question)]));

        Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id));
        Assert.Contains($"«{PgConstraints}» повторюється", error.Message);
    }

    [Fact]
    public void Every_bad_source_is_reported_together_with_other_rule_errors()
    {
        var question = Single() with
        {
            Explanation = "",
            Sources = ["http://www.postgresql.org/docs/18/", "", PgGroupBy, PgGroupBy],
        };

        var errors = _validator.Validate([WithAlphaQuestion(Field(), 0, question)]);

        // explanation, the http source, the empty item, the repeat.
        Assert.Equal(4, errors.Count);
        Assert.All(errors, error => Assert.Equal((AlphaFile, "alpha-001"), (error.File, error.Id)));
    }

    [Fact]
    public void Every_broken_rule_is_reported_not_only_the_first()
    {
        var question = new QuestionDefinition("Bad_Id", "lead", "multi", "", new CodeBlock("C#", ""), [new OptionDefinition("", true)], "", null);

        var errors = _validator.Validate([WithAlphaQuestion(Field(), 0, question)]);

        // id, level, type, option count, empty option, text, explanation, code source, code lang.
        Assert.Equal(9, errors.Count);
        Assert.All(errors, error => Assert.Equal((AlphaFile, "Bad_Id"), (error.File, error.Id)));
    }

    [Fact]
    public void Error_prints_as_file_id_message_and_skips_empty_parts()
    {
        Assert.Equal("it/topics/alpha.yaml: alpha-001: Щось не так.",
            new ContentError(AlphaFile, "alpha-001", "Щось не так.").ToString());
        Assert.Equal(".: Немає галузей.", new ContentError(".", "", "Немає галузей.").ToString());
    }

    private static List<LevelDefinition> Levels() =>
        [new("junior", "Junior"), new("middle", "Middle"), new("senior", "Senior")];

    private static QuestionDefinition Single(string id = "alpha-001") => new(
        id, "junior", "single", "Питання?", null,
        [new OptionDefinition("Так", true), new OptionDefinition("Ні", false)],
        "Бо так.", null);

    private static QuestionDefinition Multiple(string id = "alpha-002") => new(
        id, "senior", "multiple", "Які правильні?", new CodeBlock("csharp", "Run();"),
        [new OptionDefinition("Перший", true), new OptionDefinition("Другий", true), new OptionDefinition("Третій", false)],
        "Перші два.", null);

    private static List<OptionDefinition> OptionsWithFirstCorrect(int count) =>
        Enumerable.Range(1, count).Select(i => new OptionDefinition($"Варіант {i}", i == 1)).ToList();

    private static FieldDefinition Field() => new(
        FieldFile, "it", "IT", Levels(),
        [new DirectionDefinition("backend", "Backend", [new StackDefinition("dotnet", ".NET (C#)", ["alpha", "beta"])])],
        [
            UkrainianOnly(new TopicDefinition(AlphaFile, "alpha", "Alpha", "Перша тема.", [Single(), Multiple()])),
            UkrainianOnly(new TopicDefinition(BetaFile, "beta", "Beta", "", [])),
        ]);

    /// <summary><c>draft_en: true</c>: a topic without English is valid only so (rule 12, PRP-031).</summary>
    private static TopicDefinition UkrainianOnly(TopicDefinition topic) =>
        topic with { DraftLanguages = new HashSet<string> { "en" } };

    private static FieldDefinition WithAlphaQuestion(FieldDefinition field, int index, QuestionDefinition question)
    {
        var alpha = field.Topics[0];
        var questions = alpha.Questions.ToList();
        questions[index] = question;
        return field with { Topics = [alpha with { Questions = questions }, .. field.Topics.Skip(1)] };
    }

    private static FieldDefinition WithStackTopics(FieldDefinition field, string[] topicIds)
    {
        var direction = field.Directions[0];
        return field with
        {
            Directions = [direction with { Stacks = [direction.Stacks[0] with { TopicIds = topicIds }] }],
        };
    }
}

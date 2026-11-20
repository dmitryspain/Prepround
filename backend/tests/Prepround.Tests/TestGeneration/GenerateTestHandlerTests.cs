using Prepround.Application.TestGeneration;
using Prepround.Domain.Catalog;
using Prepround.Domain.Shared;

namespace Prepround.Tests.TestGeneration;

/// <summary>
/// GenerateTestHandler with a hand-written repository: every validation code of spec 5.2, the limits of the total,
/// the per-topic maximum counted after the level filter, and how the response maps options back to the file.
/// Levels here are made-up ids, so nothing depends on the names of the real scale.
/// </summary>
public class GenerateTestHandlerTests
{
    private const string FieldId = "fld";
    private const string Low = "lvl-a";
    private const string High = "lvl-b";
    private const string Top = "lvl-c";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nope")]
    public async Task Unknown_or_missing_field_fails_with_field_not_found(string? fieldId)
    {
        var result = await Handle(Query(fieldId: fieldId));

        Assert.Equal(GenerateTestErrors.FieldNotFoundCode, result.Error.Code);
    }

    [Fact]
    public async Task Inactive_field_is_not_found()
    {
        var repository = Repository();
        repository.Field.Deactivate();

        var result = await Handle(Query(), repository);

        Assert.Equal(GenerateTestErrors.FieldNotFoundCode, result.Error.Code);
    }

    [Fact]
    public async Task Missing_or_empty_levels_fail_with_levels_required()
    {
        Assert.Equal(GenerateTestErrors.LevelsRequiredCode, (await Handle(Query() with { LevelIds = null })).Error.Code);
        Assert.Equal(GenerateTestErrors.LevelsRequiredCode, (await Handle(Query(levels: []))).Error.Code);
    }

    [Theory]
    [InlineData("lvl-x")]
    [InlineData(null)]
    // Ids are compared as written: a level differing only in case is another level.
    [InlineData("LVL-A")]
    public async Task Level_outside_the_field_fails_with_unknown_level(string? level)
    {
        var result = await Handle(Query(levels: [Low, level]));

        Assert.Equal(GenerateTestErrors.UnknownLevelCode, result.Error.Code);
    }

    [Fact]
    public async Task Repeated_level_fails_with_duplicate_level()
    {
        var result = await Handle(Query(levels: [Low, High, Low]));

        Assert.Equal(GenerateTestErrors.DuplicateLevelCode, result.Error.Code);
    }

    [Fact]
    public async Task Missing_or_empty_topics_fail_with_topics_required()
    {
        Assert.Equal(GenerateTestErrors.TopicsRequiredCode, (await Handle(Query() with { Topics = null })).Error.Code);
        Assert.Equal(GenerateTestErrors.TopicsRequiredCode, (await Handle(Query(topics: []))).Error.Code);
    }

    [Fact]
    public async Task Unknown_null_or_inactive_topic_fails_with_unknown_topic()
    {
        var repository = Repository();
        repository.Topics.Single(topic => topic.Id == "gamma").Deactivate();

        Assert.Equal(GenerateTestErrors.UnknownTopicCode,
            (await Handle(Query(topics: [("alpha", 5), ("nope", 5)]), repository)).Error.Code);
        Assert.Equal(GenerateTestErrors.UnknownTopicCode,
            (await Handle(Query(topics: [("alpha", 5), (null, 5)]), repository)).Error.Code);
        Assert.Equal(GenerateTestErrors.UnknownTopicCode,
            (await Handle(Query() with { Topics = [new TestTopicRequest("alpha", 5), null] }, repository)).Error.Code);
        Assert.Equal(GenerateTestErrors.UnknownTopicCode,
            (await Handle(Query(topics: [("alpha", 5), ("gamma", 5)]), repository)).Error.Code);
    }

    [Fact]
    public async Task Repeated_topic_fails_with_duplicate_topic()
    {
        var result = await Handle(Query(topics: [("alpha", 5), ("beta", 3), ("alpha", 2)]));

        Assert.Equal(GenerateTestErrors.DuplicateTopicCode, result.Error.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Count_below_one_fails_with_count_too_small(int count)
    {
        var result = await Handle(Query(topics: [("alpha", 10), ("beta", count)]));

        Assert.Equal(GenerateTestErrors.CountTooSmallCode, result.Error.Code);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(101)]
    public async Task Total_outside_10_to_100_fails_with_total_out_of_range(int total)
    {
        var result = await Handle(Query(topics: [("alpha", total - 1), ("beta", 1)]));

        Assert.Equal(GenerateTestErrors.TotalOutOfRangeCode, result.Error.Code);
    }

    [Fact]
    public async Task Huge_counts_do_not_overflow_into_the_allowed_range()
    {
        // int.MaxValue * 2 + 12 wraps to 10 in int arithmetic.
        var result = await Handle(Query(topics: [("alpha", int.MaxValue), ("beta", int.MaxValue), ("gamma", 12)]));

        Assert.Equal(GenerateTestErrors.TotalOutOfRangeCode, result.Error.Code);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    public async Task Totals_of_10_and_100_are_accepted(int total)
    {
        var result = await Handle(Query(topics: [("gamma", total - 1), ("beta", 1)]));

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(total, result.Value.Questions.Count);
    }

    [Fact]
    public async Task Maximum_of_a_topic_counts_only_active_questions_on_the_selected_levels()
    {
        // alpha: 12 questions on each of three levels, two of them inactive on the low level.
        var repository = Repository();
        repository.Deactivate("alpha-a-01", "alpha-a-02");

        var tooMany = await Handle(Query(levels: [Low, Top], topics: [("alpha", 23), ("beta", 1)]), repository);
        var exactly = await Handle(Query(levels: [Low, Top], topics: [("alpha", 22), ("beta", 1)]), repository);

        Assert.Equal(GenerateTestErrors.NotEnoughQuestionsCode, tooMany.Error.Code);
        Assert.True(exactly.IsSuccess, exactly.IsFailure ? exactly.Error.Message : null);
        Assert.All(exactly.Value.Questions, question => Assert.NotEqual(High, question.LevelId));
        Assert.DoesNotContain(exactly.Value.Questions, question => question.Id is "alpha-a-01" or "alpha-a-02");
    }

    [Fact]
    public async Task Not_enough_questions_names_the_topic_and_what_is_available()
    {
        var result = await Handle(Query(levels: [High], topics: [("alpha", 13), ("beta", 1)]));

        Assert.Equal(GenerateTestErrors.NotEnoughQuestionsCode, result.Error.Code);
        Assert.Contains("Alpha title", result.Error.Message);
        Assert.Contains("доступно 12", result.Error.Message);
    }

    [Fact]
    public async Task Topic_with_no_questions_on_the_selected_levels_fails_with_zero_available()
    {
        // beta has questions only on the low level.
        var result = await Handle(Query(levels: [High], topics: [("alpha", 9), ("beta", 1)]));

        Assert.Equal(GenerateTestErrors.NotEnoughQuestionsCode, result.Error.Code);
        Assert.Contains("Beta title", result.Error.Message);
        Assert.Contains("доступно 0", result.Error.Message);
    }

    [Fact]
    public async Task More_than_10_000_excluded_ids_fail_with_too_many_excluded_and_10_000_pass()
    {
        var limit = Enumerable.Range(0, 10_000).Select(index => (string?)$"x-{index}").ToList();
        var over = limit.Append("x-last").ToList();

        var passed = await Handle(Query() with { ExcludeQuestionIds = limit });
        var failed = await Handle(Query() with { ExcludeQuestionIds = over });

        Assert.True(passed.IsSuccess, passed.IsFailure ? passed.Error.Message : null);
        Assert.Equal(GenerateTestErrors.TooManyExcludedCode, failed.Error.Code);
    }

    [Fact]
    public async Task Excluded_questions_are_used_only_when_the_topic_runs_out_of_others()
    {
        // alpha on the high level: 12 questions, 8 of them excluded. beta's 4 low questions fill the total to 10.
        var excluded = Enumerable.Range(5, 8).Select(index => (string?)$"alpha-b-{index:00}").ToList();
        var alphaUnseen = Enumerable.Range(1, 4).Select(index => $"alpha-b-{index:00}").ToList();

        foreach (var seed in Enumerable.Range(1, 30))
        {
            var four = await Handle(
                Query(levels: [Low, High], topics: [("alpha", 4), ("beta", 6)]) with { ExcludeQuestionIds = excluded },
                Repository(alphaLevels: [High]),
                seed);
            var six = await Handle(
                Query(levels: [Low, High], topics: [("alpha", 6), ("beta", 4)]) with { ExcludeQuestionIds = excluded },
                Repository(alphaLevels: [High]),
                seed);

            Assert.Equal(alphaUnseen, AlphaIds(four).Order());
            var alphaOfSix = AlphaIds(six);
            Assert.Equal(6, alphaOfSix.Count);
            Assert.Subset(alphaOfSix.ToHashSet(), alphaUnseen.ToHashSet());
            Assert.Equal(2, alphaOfSix.Count(id => excluded.Contains(id)));
        }

        static List<string> AlphaIds(Result<GeneratedTestResponse> result) =>
            result.Value.Questions.Where(question => question.TopicId == "alpha").Select(question => question.Id).ToList();
    }

    [Fact]
    public async Task Options_keep_their_file_index_as_key_with_text_correctness_and_code()
    {
        var repository = Repository();
        var result = await Handle(Query(), repository);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.Equal(10, result.Value.Questions.Count);
        Assert.Equal(10, result.Value.Questions.Select(question => question.Id).Distinct().Count());
        Assert.All(result.Value.Questions, question =>
        {
            var source = repository.Questions.Single(row => row.Id == question.Id);
            Assert.Equal((source.TopicId, source.LevelId, source.Text, source.Explanation),
                (question.TopicId, question.LevelId, question.Text, question.Explanation));
            Assert.Equal(source.Type == QuestionType.Single ? "single" : "multiple", question.Type);
            Assert.Equal(Enumerable.Range(0, source.Options.Count), question.Options.Select(option => option.Key).Order());
            Assert.All(question.Options, option => Assert.Equal(
                (source.Options[option.Key].Text, source.Options[option.Key].IsCorrect),
                (option.Text, option.IsCorrect)));
            if (source.CodeLang is null)
            {
                Assert.Null(question.Code);
            }
            else
            {
                Assert.Equal(new QuestionCodeResponse(source.CodeLang, source.CodeSource!), question.Code);
            }
        });
    }

    [Fact]
    public async Task Same_seed_gives_the_same_test_with_the_same_option_order()
    {
        var first = await Handle(Query(), seed: 42);
        var second = await Handle(Query(), seed: 42);

        Assert.Equal(Describe(first.Value), Describe(second.Value));

        static IEnumerable<string> Describe(GeneratedTestResponse test) =>
            test.Questions.Select(question => $"{question.Id}:{string.Join(",", question.Options.Select(option => option.Key))}");
    }

    [Fact]
    public async Task New_level_from_the_data_works_without_code_changes()
    {
        var repository = Repository();
        repository.Levels.Add(new Level(FieldId, "architect", "Architect", 4));
        repository.AddQuestions("alpha", "architect", 10);

        var result = await Handle(Query(levels: ["architect"], topics: [("alpha", 10)]), repository);

        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        Assert.All(result.Value.Questions, question => Assert.Equal("architect", question.LevelId));
    }

    [Fact]
    public async Task Cancellation_token_reaches_every_repository_call()
    {
        using var cts = new CancellationTokenSource();
        var repository = Repository();

        await new GenerateTestHandler(repository, new Random(1)).Handle(Query(), cts.Token);

        Assert.Equal(3, repository.Tokens.Count);
        Assert.All(repository.Tokens, token => Assert.Equal(cts.Token, token));
    }

    [Fact]
    public async Task Invalid_request_never_loads_full_questions()
    {
        var repository = Repository();

        await Handle(Query(topics: [("alpha", 200)]), repository);
        await Handle(Query() with { ExcludeQuestionIds = Enumerable.Repeat((string?)"x", 10_001).ToList() }, repository);

        Assert.Equal(0, repository.QuestionLoads);
    }

    private static GenerateTestQuery Query(
        string? fieldId = FieldId,
        IReadOnlyList<string?>? levels = null,
        IReadOnlyList<(string? TopicId, int Count)>? topics = null) =>
        new(
            fieldId,
            levels ?? [Low, High],
            (topics ?? [("alpha", 5), ("beta", 5)]).Select(topic => (TestTopicRequest?)new TestTopicRequest(topic.TopicId, topic.Count)).ToList(),
            null);

    private static Task<Result<GeneratedTestResponse>> Handle(
        GenerateTestQuery query, FakeTestGenerationRepository? repository = null, int seed = 1) =>
        new GenerateTestHandler(repository ?? Repository(), new Random(seed)).Handle(query, TestContext.Current.CancellationToken);

    /// <summary>
    /// Field with levels a, b, c and topics alpha (12 per level of <paramref name="alphaLevels"/>), beta (12 on the low
    /// level), gamma (120 on the low level). Options alternate code and no code, and the correct one is not first.
    /// </summary>
    private static FakeTestGenerationRepository Repository(string[]? alphaLevels = null)
    {
        var repository = new FakeTestGenerationRepository(new Field(FieldId, "Field"));
        repository.Levels.AddRange([new Level(FieldId, Low, "A", 1), new Level(FieldId, High, "B", 2), new Level(FieldId, Top, "C", 3)]);
        repository.Topics.AddRange([
            new Topic(FieldId, "alpha", "Alpha title", string.Empty),
            new Topic(FieldId, "beta", "Beta title", string.Empty),
            new Topic(FieldId, "gamma", "Gamma title", string.Empty),
        ]);
        foreach (var level in alphaLevels ?? [Low, High, Top])
        {
            repository.AddQuestions("alpha", level, 12);
        }

        repository.AddQuestions("beta", Low, 12);
        repository.AddQuestions("gamma", Low, 120);
        return repository;
    }
}

internal sealed class FakeTestGenerationRepository(Field field) : ITestGenerationRepository
{
    public Field Field { get; } = field;

    public List<Level> Levels { get; } = [];

    public List<Topic> Topics { get; } = [];

    public List<Question> Questions { get; } = [];

    public List<CancellationToken> Tokens { get; } = [];

    public int QuestionLoads { get; private set; }

    /// <summary>Ids like alpha-b-01 (topic, last letter of the level, number); every third one has a code block.</summary>
    public void AddQuestions(string topicId, string levelId, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            var id = $"{topicId}-{levelId[^1]}-{index:00}";
            var multiple = index % 2 == 0;
            Questions.Add(new Question(
                id, Field.Id, topicId, levelId,
                multiple ? QuestionType.Multiple : QuestionType.Single,
                $"Text of {id}",
                index % 3 == 0 ? "csharp" : null,
                index % 3 == 0 ? $"// code of {id}" : null,
                [
                    new QuestionOption($"{id} option 0", false),
                    new QuestionOption($"{id} option 1", true),
                    new QuestionOption($"{id} option 2", multiple),
                    new QuestionOption($"{id} option 3", false),
                ],
                $"Explanation of {id}",
                DateTimeOffset.UnixEpoch));
        }
    }

    public void Deactivate(params string[] ids)
    {
        foreach (var question in Questions.Where(question => ids.Contains(question.Id)))
        {
            question.Deactivate();
        }
    }

    public Task<ActiveField?> GetActiveFieldAsync(string fieldId, CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        return Task.FromResult(fieldId == Field.Id && Field.IsActive
            ? new ActiveField(Field, Levels.Where(level => level.IsActive).ToList(), Topics.Where(topic => topic.IsActive).ToList())
            : null);
    }

    public Task<IReadOnlyList<QuestionCandidate>> GetCandidatesAsync(
        string fieldId, IReadOnlyCollection<string> topicIds, string language, CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        IReadOnlyList<QuestionCandidate> candidates = Questions
            .Where(question => question.FieldId == fieldId && topicIds.Contains(question.TopicId))
            .Select(question => new QuestionCandidate(question.Id, question.TopicId, question.LevelId, question.IsActive))
            .ToList();
        return Task.FromResult(candidates);
    }

    public Task<IReadOnlyList<Question>> GetQuestionsAsync(IReadOnlyCollection<string> questionIds, CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        QuestionLoads++;
        // Reverse order on purpose: the repository promises no order, so the handler must not rely on one.
        IReadOnlyList<Question> questions = Questions.Where(question => questionIds.Contains(question.Id)).Reverse().ToList();
        return Task.FromResult(questions);
    }

    public Task<IReadOnlyList<QuestionTranslation>> GetTranslationsAsync(
        IReadOnlyCollection<string> questionIds, string language, CancellationToken cancellationToken)
    {
        Tokens.Add(cancellationToken);
        return Task.FromResult<IReadOnlyList<QuestionTranslation>>([]);
    }
}

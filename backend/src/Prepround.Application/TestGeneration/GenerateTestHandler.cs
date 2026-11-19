using Prepround.Application.Abstractions.Messaging;
using Prepround.Domain.Catalog;
using Prepround.Domain.Shared;

namespace Prepround.Application.TestGeneration;

/// <summary>
/// Builds a test by the rules of spec 5.2: validates the request in the order of the spec, stopping at the first
/// violation, then picks active questions of the selected topics and levels, preferring those not seen yet.
/// </summary>
public sealed class GenerateTestHandler : IQueryHandler<GenerateTestQuery, GeneratedTestResponse>
{
    public const int MinTotalCount = 10;
    public const int MaxTotalCount = 100;
    public const int MaxExcludedCount = 10_000;

    private readonly ITestGenerationRepository _repository;
    private readonly Random _random;

    public GenerateTestHandler(ITestGenerationRepository repository, Random random)
    {
        _repository = repository;
        _random = random;
    }

    public async Task<Result<GeneratedTestResponse>> Handle(GenerateTestQuery query, CancellationToken cancellationToken)
    {
        var field = string.IsNullOrEmpty(query.FieldId)
            ? null
            : await _repository.GetActiveFieldAsync(query.FieldId, cancellationToken);
        if (field is null)
        {
            return Result.Failure<GeneratedTestResponse>(GenerateTestErrors.FieldNotFound(query.FieldId));
        }

        var levels = SelectLevels(query.LevelIds, field);
        if (levels.IsFailure)
        {
            return Result.Failure<GeneratedTestResponse>(levels.Error);
        }

        var topics = SelectTopics(query.Topics, field);
        if (topics.IsFailure)
        {
            return Result.Failure<GeneratedTestResponse>(topics.Error);
        }

        var tooSmall = topics.Value.FirstOrDefault(topic => topic.Count < 1);
        if (tooSmall is not null)
        {
            return Result.Failure<GeneratedTestResponse>(GenerateTestErrors.CountTooSmall(tooSmall.Topic, tooSmall.Count));
        }

        // Long, so a sum of huge counts cannot overflow into the allowed range.
        var total = topics.Value.Sum(topic => (long)topic.Count);
        if (total is < MinTotalCount or > MaxTotalCount)
        {
            return Result.Failure<GeneratedTestResponse>(
                GenerateTestErrors.TotalOutOfRange(total, MinTotalCount, MaxTotalCount));
        }

        // Only active questions on the selected levels count, both for the limit and for the pick.
        var candidates = await _repository.GetCandidatesAsync(
            field.Field.Id,
            topics.Value.Select(topic => topic.Topic.Id).ToList(),
            cancellationToken);
        var available = candidates
            .Where(candidate => candidate.IsActive && levels.Value.Contains(candidate.LevelId))
            .ToLookup(candidate => candidate.TopicId, StringComparer.Ordinal);

        foreach (var topic in topics.Value)
        {
            var availableCount = available[topic.Topic.Id].Count();
            if (topic.Count > availableCount)
            {
                return Result.Failure<GeneratedTestResponse>(
                    GenerateTestErrors.NotEnoughQuestions(topic.Topic, topic.Count, availableCount));
            }
        }

        // Counted as sent, duplicates included: the limit is on the request size, not on distinct ids.
        if (query.ExcludeQuestionIds is { Count: > MaxExcludedCount })
        {
            return Result.Failure<GeneratedTestResponse>(
                GenerateTestErrors.TooManyExcluded(query.ExcludeQuestionIds.Count, MaxExcludedCount));
        }

        var excluded = (query.ExcludeQuestionIds ?? []).OfType<string>().ToHashSet(StringComparer.Ordinal);
        var picks = topics.Value
            .Select(topic => new TopicPick(
                available[topic.Topic.Id]
                    .Select(candidate => new PickCandidate(candidate.Id, excluded.Contains(candidate.Id)))
                    .ToList(),
                topic.Count))
            .ToList();
        var questionIds = QuestionPicker.Pick(picks, _random);

        var questions = (await _repository.GetQuestionsAsync(questionIds, cancellationToken))
            .ToDictionary(question => question.Id, StringComparer.Ordinal);

        return new GeneratedTestResponse(questionIds.Select(id => ToResponse(questions[id])).ToList());
    }

    private static Result<HashSet<string>> SelectLevels(IReadOnlyList<string?>? levelIds, ActiveField field)
    {
        if (levelIds is null || levelIds.Count == 0)
        {
            return Result.Failure<HashSet<string>>(GenerateTestErrors.LevelsRequired);
        }

        var known = field.Levels.Select(level => level.Id).ToHashSet(StringComparer.Ordinal);
        var selected = new HashSet<string>(StringComparer.Ordinal);

        foreach (var levelId in levelIds)
        {
            if (levelId is null || !known.Contains(levelId))
            {
                return Result.Failure<HashSet<string>>(GenerateTestErrors.UnknownLevel(levelId, field.Field.Id));
            }

            if (!selected.Add(levelId))
            {
                return Result.Failure<HashSet<string>>(GenerateTestErrors.DuplicateLevel(levelId));
            }
        }

        return selected;
    }

    /// <returns>The requested topics in request order, each resolved to its active row.</returns>
    private static Result<List<SelectedTopic>> SelectTopics(IReadOnlyList<TestTopicRequest?>? requested, ActiveField field)
    {
        if (requested is null || requested.Count == 0)
        {
            return Result.Failure<List<SelectedTopic>>(GenerateTestErrors.TopicsRequired);
        }

        var known = field.Topics.ToDictionary(topic => topic.Id, StringComparer.Ordinal);
        var selectedIds = new HashSet<string>(StringComparer.Ordinal);
        var selected = new List<SelectedTopic>();

        foreach (var item in requested)
        {
            if (item?.TopicId is not { } topicId || !known.TryGetValue(topicId, out var topic))
            {
                return Result.Failure<List<SelectedTopic>>(GenerateTestErrors.UnknownTopic(item?.TopicId, field.Field.Id));
            }

            if (!selectedIds.Add(topicId))
            {
                return Result.Failure<List<SelectedTopic>>(GenerateTestErrors.DuplicateTopic(topicId));
            }

            selected.Add(new SelectedTopic(topic, item.Count));
        }

        return selected;
    }

    private TestQuestionResponse ToResponse(Question question) => new(
        question.Id,
        question.TopicId,
        question.LevelId,
        // The same words as in the content files and in the database: single, multiple.
        question.Type.ToString().ToLowerInvariant(),
        question.Text,
        question is { CodeLang: { } lang, CodeSource: { } source } ? new QuestionCodeResponse(lang, source) : null,
        QuestionPicker.ShuffleOptionKeys(question.Options.Count, _random)
            .Select(key => new TestOptionResponse(key, question.Options[key].Text, question.Options[key].IsCorrect))
            .ToList(),
        question.Explanation);

    private sealed record SelectedTopic(Topic Topic, int Count);
}

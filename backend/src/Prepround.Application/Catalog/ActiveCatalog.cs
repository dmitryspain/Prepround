using Prepround.Domain.Catalog;

namespace Prepround.Application.Catalog;

/// <summary>Active catalog rows only, as stored: the handler shapes and orders them.</summary>
/// <param name="QuestionCounts">Active questions grouped by topic and level; groups without questions are absent.</param>
public sealed record ActiveCatalog(
    IReadOnlyList<Field> Fields,
    IReadOnlyList<Level> Levels,
    IReadOnlyList<Direction> Directions,
    IReadOnlyList<Stack> Stacks,
    IReadOnlyList<StackTopic> StackTopics,
    IReadOnlyList<Topic> Topics,
    IReadOnlyList<TopicLevelCount> QuestionCounts);

public sealed record TopicLevelCount(string FieldId, string TopicId, string LevelId, int Count);

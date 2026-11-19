using Prepround.Domain.Catalog;

namespace Prepround.Application.Content.Import;

/// <summary>Catalog rows of every type: all rows in the database, or only the new ones to insert.</summary>
public sealed record CatalogRows(
    IReadOnlyList<Field> Fields,
    IReadOnlyList<Level> Levels,
    IReadOnlyList<Direction> Directions,
    IReadOnlyList<Stack> Stacks,
    IReadOnlyList<StackTopic> StackTopics,
    IReadOnlyList<Topic> Topics,
    IReadOnlyList<Question> Questions);

namespace Prepround.Application.Catalog;

/// <summary>Shape of <c>GET /api/catalog</c> (spec 5.1); the API serializes it in camelCase.</summary>
public sealed record CatalogResponse(IReadOnlyList<FieldResponse> Fields);

/// <param name="Topics">Every active topic of the field, sorted by the title shown; stacks refer to them by id.</param>
public sealed record FieldResponse(
    string Id,
    string Title,
    IReadOnlyList<LevelResponse> Levels,
    IReadOnlyList<DirectionResponse> Directions,
    IReadOnlyList<TopicResponse> Topics);

/// <param name="Rank">1-based position in the field's scale, from the lowest level to the highest.</param>
public sealed record LevelResponse(string Id, string Title, int Rank);

public sealed record DirectionResponse(string Id, string Title, IReadOnlyList<StackResponse> Stacks);

/// <param name="TopicIds">Active topics of the stack, in file order.</param>
public sealed record StackResponse(string Id, string Title, IReadOnlyList<string> TopicIds);

/// <param name="Description">Empty when the topic file has none.</param>
/// <param name="QuestionCounts">
/// Active questions available in the language of the request, by level id, in scale order; a level without questions
/// is absent, so a topic without any gives {}.
/// The client sums the selected levels to get the maximum count for the topic.
/// </param>
public sealed record TopicResponse(
    string Id,
    string Title,
    string Description,
    IReadOnlyDictionary<string, int> QuestionCounts);

using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content;
using Prepround.Domain.Catalog;
using Prepround.Domain.Shared;

namespace Prepround.Application.Catalog;

/// <summary>
/// Builds the catalog of spec 5.1 from active rows. An active row under an inactive parent is dropped as well,
/// so the client never sees a stack, topic id or level id it cannot resolve. Titles come in the language of the
/// request, each one falling back to the base language on its own (ADR 006, section 3).
/// </summary>
public sealed class GetCatalogHandler : IQueryHandler<GetCatalogQuery, CatalogResponse>
{
    /// <summary>
    /// Linguistic and culture-invariant: titles mix Latin and Cyrillic, and the order must not depend on the server's
    /// culture or the database collation. Ties fall back to the ordinal id, so the order is always the same.
    /// </summary>
    private static readonly StringComparer TitleComparer = StringComparer.InvariantCulture;

    private readonly ICatalogReadRepository _repository;

    public GetCatalogHandler(ICatalogReadRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CatalogResponse>> Handle(GetCatalogQuery query, CancellationToken cancellationToken)
    {
        var language = RequestLanguage.Resolve(query.Language);
        if (language.IsFailure)
        {
            return Result.Failure<CatalogResponse>(language.Error);
        }

        var catalog = await _repository.GetActiveAsync(language.Value, cancellationToken);
        var rows = new CatalogLookups(catalog);

        // Sorted by the title shown, so the order follows the language of the request.
        var fields = catalog.Fields
            .Select(field => ToResponse(field, rows, language.Value))
            .OrderBy(field => field.Title, TitleComparer)
            .ThenBy(field => field.Id, StringComparer.Ordinal)
            .ToList();

        return new CatalogResponse(fields);
    }

    private static FieldResponse ToResponse(Field field, CatalogLookups rows, string language)
    {
        // Rank is not unique (a deactivated level keeps its old one), so the id breaks ties among active levels too.
        var levels = rows.Levels[field.Id]
            .OrderBy(level => level.Rank)
            .ThenBy(level => level.Id, StringComparer.Ordinal)
            .ToList();
        var topics = rows.Topics[field.Id]
            .Select(topic => new TopicResponse(
                topic.Id,
                Localized(topic.Title, topic.TitleTranslations, language),
                Localized(topic.Description, topic.DescriptionTranslations, language),
                QuestionCounts(rows.QuestionCounts[(field.Id, topic.Id)], levels)))
            .OrderBy(topic => topic.Title, TitleComparer)
            .ThenBy(topic => topic.Id, StringComparer.Ordinal)
            .ToList();
        var activeTopicIds = topics.Select(topic => topic.Id).ToHashSet(StringComparer.Ordinal);

        var directions = rows.Directions[field.Id]
            .OrderBy(direction => direction.Order)
            .ThenBy(direction => direction.Id, StringComparer.Ordinal)
            .Select(direction => new DirectionResponse(
                direction.Id,
                Localized(direction.Title, direction.TitleTranslations, language),
                rows.Stacks[(field.Id, direction.Id)]
                    .OrderBy(stack => stack.Order)
                    .ThenBy(stack => stack.Id, StringComparer.Ordinal)
                    .Select(stack => new StackResponse(
                        stack.Id,
                        Localized(stack.Title, stack.TitleTranslations, language),
                        rows.StackTopics[(field.Id, stack.Id)]
                            .Where(link => activeTopicIds.Contains(link.TopicId))
                            .OrderBy(link => link.Order)
                            .ThenBy(link => link.TopicId, StringComparer.Ordinal)
                            .Select(link => link.TopicId)
                            .ToList()))
                    .ToList()))
            .ToList();

        return new FieldResponse(
            field.Id,
            Localized(field.Title, field.TitleTranslations, language),
            levels
                .Select(level => new LevelResponse(
                    level.Id, Localized(level.Title, level.TitleTranslations, language), level.Rank))
                .ToList(),
            directions,
            topics);
    }

    /// <summary>
    /// The text in the language of the request, or the base text when that language has no translation of it.
    /// The base language reads the base text only, so its response stays exactly what it was before translations.
    /// </summary>
    private static string Localized(string text, IReadOnlyDictionary<string, string> translations, string language) =>
        language != ContentLanguages.Base && translations.TryGetValue(language, out var translated) ? translated : text;

    /// <summary>Counts keyed by active level ids only, in scale order; a level without questions is left out.</summary>
    private static Dictionary<string, int> QuestionCounts(IEnumerable<TopicLevelCount> counts, IReadOnlyList<Level> levels)
    {
        var byLevel = counts.ToDictionary(count => count.LevelId, count => count.Count, StringComparer.Ordinal);
        var result = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var level in levels)
        {
            if (byLevel.TryGetValue(level.Id, out var count) && count > 0)
            {
                result[level.Id] = count;
            }
        }

        return result;
    }

    /// <summary>Rows grouped by their parent key; a missing key gives an empty group, so orphans simply never show.</summary>
    private sealed class CatalogLookups
    {
        public CatalogLookups(ActiveCatalog catalog)
        {
            Levels = catalog.Levels.ToLookup(level => level.FieldId, StringComparer.Ordinal);
            Directions = catalog.Directions.ToLookup(direction => direction.FieldId, StringComparer.Ordinal);
            Stacks = catalog.Stacks.ToLookup(stack => (stack.FieldId, stack.DirectionId));
            StackTopics = catalog.StackTopics.ToLookup(link => (link.FieldId, link.StackId));
            Topics = catalog.Topics.ToLookup(topic => topic.FieldId, StringComparer.Ordinal);
            QuestionCounts = catalog.QuestionCounts.ToLookup(count => (count.FieldId, count.TopicId));
        }

        public ILookup<string, Level> Levels { get; }

        public ILookup<string, Direction> Directions { get; }

        public ILookup<(string FieldId, string DirectionId), Stack> Stacks { get; }

        public ILookup<(string FieldId, string StackId), StackTopic> StackTopics { get; }

        public ILookup<string, Topic> Topics { get; }

        public ILookup<(string FieldId, string TopicId), TopicLevelCount> QuestionCounts { get; }
    }
}

namespace Prepround.Application.Content;

/// <summary>A stack is only an ordered set of default topics; the topics themselves belong to the field.</summary>
public sealed record StackDefinition(string Id, string Title, IReadOnlyList<string> TopicIds)
{
    public TranslatedTexts TitleTranslations { get; init; } = TranslatedTexts.None;

    /// <summary>Logo slug (rule 13); null when the key is absent, empty when it is there without a value.</summary>
    public string? Icon { get; init; }

    /// <summary>The cell's "chemical symbol" (rule 13); null when the key is absent, empty when it has no value.</summary>
    public string? Symbol { get; init; }
}

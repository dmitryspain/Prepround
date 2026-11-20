namespace Prepround.Application.Content;

/// <summary>A stack is only an ordered set of default topics; the topics themselves belong to the field.</summary>
public sealed record StackDefinition(string Id, string Title, IReadOnlyList<string> TopicIds)
{
    public TranslatedTexts TitleTranslations { get; init; } = TranslatedTexts.None;
}

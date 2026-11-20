namespace Prepround.Application.Content;

public sealed record DirectionDefinition(string Id, string Title, IReadOnlyList<StackDefinition> Stacks)
{
    public TranslatedTexts TitleTranslations { get; init; } = TranslatedTexts.None;
}

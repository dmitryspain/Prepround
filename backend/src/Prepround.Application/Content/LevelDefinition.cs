namespace Prepround.Application.Content;

public sealed record LevelDefinition(string Id, string Title)
{
    public TranslatedTexts TitleTranslations { get; init; } = TranslatedTexts.None;
}

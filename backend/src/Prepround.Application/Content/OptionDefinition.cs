namespace Prepround.Application.Content;

public sealed record OptionDefinition(string Text, bool IsCorrect)
{
    /// <summary>The option's text per translation language; whether it is correct is shared by all of them (ADR 006).</summary>
    public TranslatedTexts TextTranslations { get; init; } = TranslatedTexts.None;
}

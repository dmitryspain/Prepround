namespace Prepround.Application.Content;

/// <summary>
/// A question as written in the topic file. Type and level stay raw strings here,
/// so the validator can report a wrong value instead of the reader failing on it.
/// </summary>
/// <param name="Options">In file order: the index of an option is its stable key (spec 5.2).</param>
/// <param name="Sources">
/// Documentation pages that back the correct answer (ADR 004). Null when the key is absent, which is valid;
/// an empty list means the key is present but empty, which rule 10 rejects. Lives only in YAML: import ignores it.
/// </param>
public sealed record QuestionDefinition(
    string Id,
    string LevelId,
    string Type,
    string Text,
    CodeBlock? Code,
    IReadOnlyList<OptionDefinition> Options,
    string Explanation,
    IReadOnlyList<string>? Sources)
{
    public TranslatedTexts TextTranslations { get; init; } = TranslatedTexts.None;

    public TranslatedTexts ExplanationTranslations { get; init; } = TranslatedTexts.None;

    /// <summary>
    /// Whether the question has every text in the language: its own, the explanation and each option's (rule 11).
    /// Level, type, code and which options are correct are shared, so they need no translation.
    /// </summary>
    public bool IsTranslatedTo(string language) =>
        TextTranslations.ContainsKey(language)
        && ExplanationTranslations.ContainsKey(language)
        && Options.All(option => option.TextTranslations.ContainsKey(language));
}

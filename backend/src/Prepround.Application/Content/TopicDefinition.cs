using System.Collections.Frozen;

namespace Prepround.Application.Content;

/// <param name="File">Path of the topic file relative to the content root, with forward slashes; used in errors and for rule 3.</param>
/// <param name="IsDraft">
/// The topic's questions have not passed the fact check yet (ADR 002, "draft topics"). Validated like any other topic;
/// import leaves its questions out unless drafts are included explicitly. Absent in the file means false.
/// </param>
public sealed record TopicDefinition(
    string File,
    string Id,
    string Title,
    string Description,
    IReadOnlyList<QuestionDefinition> Questions,
    bool IsDraft = false)
{
    public TranslatedTexts TitleTranslations { get; init; } = TranslatedTexts.None;

    public TranslatedTexts DescriptionTranslations { get; init; } = TranslatedTexts.None;

    /// <summary>Logo slug (rule 13); null when the key is absent, empty when it is there without a value.</summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Translation languages marked <c>draft_L: true</c>: the questions' translation into them has not passed
    /// the fact check yet (ADR 006). Unlike <see cref="IsDraft"/>, it does not touch the base-language questions
    /// or the topic's titles.
    /// </summary>
    public IReadOnlySet<string> DraftLanguages { get; init; } = FrozenSet<string>.Empty;
}

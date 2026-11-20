namespace Prepround.Application.Content.Import;

/// <summary>What an import changed, per entity type. A repeated import of the same files gives all zeros.</summary>
/// <remarks>
/// A change in a stack's topic list or its order counts as an update of the stack. Question translations have
/// counts of their own, so <see cref="Questions"/> shows whether a translation left the base content as it was (ADR 006).
/// </remarks>
public sealed record ImportSummary(
    ImportCounts Fields,
    ImportCounts Levels,
    ImportCounts Directions,
    ImportCounts Stacks,
    ImportCounts Topics,
    ImportCounts Questions,
    TranslationCounts QuestionTranslations);

/// <param name="Updated">Existing rows whose values changed or that were reactivated.</param>
public sealed record ImportCounts(int Added, int Updated, int Deactivated);

/// <param name="Updated">Existing translations whose texts changed.</param>
/// <param name="Removed">Translations deleted: no longer in the files, or their question or language left out of the import.</param>
public sealed record TranslationCounts(int Added, int Updated, int Removed);

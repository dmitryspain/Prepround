using System.Collections.ObjectModel;

namespace Prepround.Domain.Catalog;

/// <summary>
/// A catalog text in the translation languages, language → text (ADR 006). The base-language text stays in the
/// entity's own property, so it is never in the dictionary; a language without a text shows the base one.
/// </summary>
public static class Translations
{
    /// <summary>No translation at all.</summary>
    public static readonly IReadOnlyDictionary<string, string> None = ReadOnlyDictionary<string, string>.Empty;

    /// <summary>
    /// Equal by content: the same languages with the same texts. The order of the keys is not part of the value,
    /// since jsonb does not keep it either.
    /// </summary>
    public static bool Equal(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right) =>
        left.Count == right.Count
        && left.All(pair => right.TryGetValue(pair.Key, out var text) && text == pair.Value);
}

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Prepround.Application.Content;

/// <summary>
/// One content value in the translation languages, language → text (ADR 006). The base-language text is the
/// definition's own property, so it is never here. A key that is present with a blank text stays, for rule 11
/// to report. Equal by content, so the definitions that carry it keep comparing by value.
/// </summary>
public sealed class TranslatedTexts : IReadOnlyDictionary<string, string>, IEquatable<TranslatedTexts>
{
    /// <summary>The value has no translation.</summary>
    public static readonly TranslatedTexts None = new([]);

    private readonly Dictionary<string, string> _texts;

    public TranslatedTexts(IEnumerable<KeyValuePair<string, string>> texts) =>
        _texts = new Dictionary<string, string>(texts, StringComparer.Ordinal);

    public int Count => _texts.Count;

    public IEnumerable<string> Keys => _texts.Keys;

    public IEnumerable<string> Values => _texts.Values;

    public string this[string language] => _texts[language];

    public bool ContainsKey(string language) => _texts.ContainsKey(language);

    public bool TryGetValue(string language, [MaybeNullWhen(false)] out string text) =>
        _texts.TryGetValue(language, out text);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _texts.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(TranslatedTexts? other) =>
        other is not null
        && Count == other.Count
        && _texts.All(pair => other.TryGetValue(pair.Key, out var text) && text == pair.Value);

    public override bool Equals(object? obj) => Equals(obj as TranslatedTexts);

    // XOR keeps the hash independent of the order the languages were added in, as equality is.
    public override int GetHashCode() =>
        _texts.Aggregate(0, (hash, pair) => hash ^ HashCode.Combine(pair.Key, pair.Value));

    public override string ToString() =>
        $"{{ {string.Join(", ", _texts.Select(pair => $"{pair.Key}: {pair.Value}"))} }}";
}

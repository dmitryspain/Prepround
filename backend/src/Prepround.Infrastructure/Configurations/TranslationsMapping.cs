using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Configurations;

/// <summary>The jsonb column of a catalog text's translations, such as <c>{"en": "…"}</c> (ADR 006, section 2).</summary>
internal static class TranslationsMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    // By value and regardless of key order, like Translations.Equal: jsonb reorders keys and changes escaping,
    // so comparing the serialized text would report a change on every repeated import.
    private static readonly ValueComparer<IReadOnlyDictionary<string, string>> Comparer = new(
        (left, right) => Translations.Equal(left!, right!),
        texts => texts.Aggregate(0, (hash, pair) => hash ^ HashCode.Combine(pair.Key, pair.Value)),
        texts => new Dictionary<string, string>(texts, StringComparer.Ordinal));

    /// <summary>
    /// NOT NULL with the default <c>{}</c>: rows written before translations existed, or by code that does not
    /// know the column yet, mean "no translation" (ADR 006, section 2).
    /// </summary>
    public static PropertyBuilder<IReadOnlyDictionary<string, string>> HasTranslationsColumn(
        this PropertyBuilder<IReadOnlyDictionary<string, string>> property) =>
        property
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .HasConversion(
                texts => JsonSerializer.Serialize(texts, Json),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json)!,
                Comparer);
}

using Microsoft.EntityFrameworkCore;
using Prepround.Domain.Catalog;
using Prepround.Tests.Infrastructure;

namespace Prepround.Tests.Domain;

/// <summary>
/// Catalog title translations compare by value (PRP-026, ADR 006): jsonb gives keys back in its own order, so the order
/// must never make a repeated import report an update or make EF write the column again.
/// </summary>
public class TranslationsTests
{
    private static readonly Dictionary<string, string> EnThenDe = new() { ["en"] = "SQL", ["de"] = "SQL (de)" };
    private static readonly Dictionary<string, string> DeThenEn = new() { ["de"] = "SQL (de)", ["en"] = "SQL" };

    [Fact]
    public void Equal_ignores_the_order_of_the_languages_and_not_their_texts()
    {
        Assert.True(Translations.Equal(EnThenDe, DeThenEn));
        Assert.True(Translations.Equal(Translations.None, new Dictionary<string, string>()));
        Assert.False(Translations.Equal(EnThenDe, new Dictionary<string, string> { ["en"] = "SQL", ["de"] = "Other" }));
        Assert.False(Translations.Equal(EnThenDe, new Dictionary<string, string> { ["en"] = "SQL" }));
        Assert.False(Translations.Equal(new Dictionary<string, string> { ["en"] = "SQL" }, EnThenDe));
        Assert.False(Translations.Equal(Translations.None, new Dictionary<string, string> { ["en"] = "SQL" }));
    }

    [Fact]
    public void Topic_with_the_same_translations_in_another_key_order_is_not_updated()
    {
        var topic = new Topic("it", "sql", "SQL", "Опис", EnThenDe, EnThenDe);

        Assert.False(topic.UpdateFrom(new Topic("it", "sql", "SQL", "Опис", DeThenEn, DeThenEn)));
        Assert.True(topic.UpdateFrom(new Topic("it", "sql", "SQL", "Опис", DeThenEn, Translations.None)));
        Assert.True(new Level("it", "junior", "Junior", 1).UpdateFrom(new Level("it", "junior", "Junior", 1, EnThenDe)));
    }

    [Theory]
    [InlineData(typeof(Field), nameof(Field.TitleTranslations))]
    [InlineData(typeof(Level), nameof(Level.TitleTranslations))]
    [InlineData(typeof(Direction), nameof(Direction.TitleTranslations))]
    [InlineData(typeof(Stack), nameof(Stack.TitleTranslations))]
    [InlineData(typeof(Topic), nameof(Topic.TitleTranslations))]
    [InlineData(typeof(Topic), nameof(Topic.DescriptionTranslations))]
    public void Ef_compares_a_translations_column_by_value_regardless_of_key_order(Type entity, string property)
    {
        // Building the model needs no server.
        using var context = PostgresFixture.CreateContext("Host=localhost;Database=model-only");
        var comparer = context.Model.FindEntityType(entity)!.FindProperty(property)!.GetValueComparer();

        Assert.Equal("jsonb", context.Model.FindEntityType(entity)!.FindProperty(property)!.GetColumnType());
        Assert.True(comparer.Equals(EnThenDe, DeThenEn));
        Assert.Equal(comparer.GetHashCode(EnThenDe), comparer.GetHashCode(DeThenEn));
        Assert.False(comparer.Equals(EnThenDe, new Dictionary<string, string> { ["en"] = "SQL", ["de"] = "Other" }));
        // The snapshot is a copy, so a later change of the source dictionary would still be detected.
        var snapshot = comparer.Snapshot(EnThenDe);
        Assert.NotSame(EnThenDe, snapshot);
        Assert.True(comparer.Equals(EnThenDe, snapshot));
    }
}

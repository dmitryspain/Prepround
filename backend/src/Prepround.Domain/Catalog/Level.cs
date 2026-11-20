namespace Prepround.Domain.Catalog;

/// <summary>A step of the field's level scale. Levels are data: their names never appear in code.</summary>
/// <remarks>Rank is the 1-based position in the scale, from the lowest level to the highest.</remarks>
public sealed class Level : CatalogEntity
{
    /// <param name="titleTranslations">Null when the title has no translation.</param>
    public Level(
        string fieldId, string id, string title, int rank, IReadOnlyDictionary<string, string>? titleTranslations = null)
    {
        FieldId = fieldId;
        Id = id;
        Title = title;
        Rank = rank;
        TitleTranslations = titleTranslations ?? Translations.None;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string Title { get; private set; }

    public int Rank { get; private set; }

    /// <summary>The title in the translation languages; a language missing here shows <see cref="Title"/> (ADR 006).</summary>
    public IReadOnlyDictionary<string, string> TitleTranslations { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the level.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Level source)
    {
        var changed = Activate()
                      | Title != source.Title
                      | Rank != source.Rank
                      | !Translations.Equal(TitleTranslations, source.TitleTranslations);
        Title = source.Title;
        Rank = source.Rank;
        TitleTranslations = source.TitleTranslations;
        return changed;
    }
}

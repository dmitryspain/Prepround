namespace Prepround.Domain.Catalog;

/// <summary>A field such as IT: owns its level scale, directions and topics.</summary>
public sealed class Field : CatalogEntity
{
    /// <param name="titleTranslations">Null when the title has no translation.</param>
    public Field(string id, string title, IReadOnlyDictionary<string, string>? titleTranslations = null)
    {
        Id = id;
        Title = title;
        TitleTranslations = titleTranslations ?? Translations.None;
    }

    public string Id { get; private set; }

    public string Title { get; private set; }

    /// <summary>The title in the translation languages; a language missing here shows <see cref="Title"/> (ADR 006).</summary>
    public IReadOnlyDictionary<string, string> TitleTranslations { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the field.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Field source)
    {
        var changed = Activate()
                      | Title != source.Title
                      | !Translations.Equal(TitleTranslations, source.TitleTranslations);
        Title = source.Title;
        TitleTranslations = source.TitleTranslations;
        return changed;
    }
}

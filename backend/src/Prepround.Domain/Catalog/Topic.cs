namespace Prepround.Domain.Catalog;

/// <summary>A topic of the field's catalog, shared by every stack that lists it.</summary>
public sealed class Topic : CatalogEntity
{
    /// <param name="titleTranslations">Null when the title has no translation.</param>
    /// <param name="descriptionTranslations">Null when the description has no translation.</param>
    /// <param name="icon">Null when the file gives none.</param>
    public Topic(
        string fieldId,
        string id,
        string title,
        string description,
        IReadOnlyDictionary<string, string>? titleTranslations = null,
        IReadOnlyDictionary<string, string>? descriptionTranslations = null,
        string? icon = null)
    {
        FieldId = fieldId;
        Id = id;
        Title = title;
        Description = description;
        TitleTranslations = titleTranslations ?? Translations.None;
        DescriptionTranslations = descriptionTranslations ?? Translations.None;
        Icon = icon;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string Title { get; private set; }

    /// <summary>Optional in the files; empty when absent.</summary>
    public string Description { get; private set; }

    /// <summary>The title in the translation languages; a language missing here shows <see cref="Title"/> (ADR 006).</summary>
    public IReadOnlyDictionary<string, string> TitleTranslations { get; private set; }

    /// <summary>The description in the translation languages; a language missing here shows <see cref="Description"/>.</summary>
    public IReadOnlyDictionary<string, string> DescriptionTranslations { get; private set; }

    /// <summary>Logo slug (a simple-icons name); the client maps it to an image file, so it is never translated.</summary>
    public string? Icon { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the topic.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Topic source)
    {
        var changed = Activate()
                      | Title != source.Title
                      | Description != source.Description
                      | !Translations.Equal(TitleTranslations, source.TitleTranslations)
                      | !Translations.Equal(DescriptionTranslations, source.DescriptionTranslations)
                      | Icon != source.Icon;
        Title = source.Title;
        Description = source.Description;
        TitleTranslations = source.TitleTranslations;
        DescriptionTranslations = source.DescriptionTranslations;
        Icon = source.Icon;
        return changed;
    }
}

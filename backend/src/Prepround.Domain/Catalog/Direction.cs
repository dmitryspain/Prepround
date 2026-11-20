namespace Prepround.Domain.Catalog;

/// <summary>A direction of a field, such as Backend.</summary>
/// <remarks>Order is the 1-based position in the field file.</remarks>
public sealed class Direction : CatalogEntity
{
    /// <param name="titleTranslations">Null when the title has no translation.</param>
    public Direction(
        string fieldId, string id, string title, int order, IReadOnlyDictionary<string, string>? titleTranslations = null)
    {
        FieldId = fieldId;
        Id = id;
        Title = title;
        Order = order;
        TitleTranslations = titleTranslations ?? Translations.None;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string Title { get; private set; }

    public int Order { get; private set; }

    /// <summary>The title in the translation languages; a language missing here shows <see cref="Title"/> (ADR 006).</summary>
    public IReadOnlyDictionary<string, string> TitleTranslations { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the direction.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Direction source)
    {
        var changed = Activate()
                      | Title != source.Title
                      | Order != source.Order
                      | !Translations.Equal(TitleTranslations, source.TitleTranslations);
        Title = source.Title;
        Order = source.Order;
        TitleTranslations = source.TitleTranslations;
        return changed;
    }
}

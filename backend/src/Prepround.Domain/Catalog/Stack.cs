namespace Prepround.Domain.Catalog;

/// <summary>A stack is only an ordered set of default topics (<see cref="StackTopic"/>); topics belong to the field.</summary>
/// <remarks>Order is the 1-based position within its direction in the field file.</remarks>
public sealed class Stack : CatalogEntity
{
    /// <param name="titleTranslations">Null when the title has no translation.</param>
    public Stack(
        string fieldId,
        string id,
        string directionId,
        string title,
        int order,
        IReadOnlyDictionary<string, string>? titleTranslations = null)
    {
        FieldId = fieldId;
        Id = id;
        DirectionId = directionId;
        Title = title;
        Order = order;
        TitleTranslations = titleTranslations ?? Translations.None;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string DirectionId { get; private set; }

    public string Title { get; private set; }

    public int Order { get; private set; }

    /// <summary>The title in the translation languages; a language missing here shows <see cref="Title"/> (ADR 006).</summary>
    public IReadOnlyDictionary<string, string> TitleTranslations { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the stack.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Stack source)
    {
        var changed = Activate()
                      | DirectionId != source.DirectionId
                      | Title != source.Title
                      | Order != source.Order
                      | !Translations.Equal(TitleTranslations, source.TitleTranslations);
        DirectionId = source.DirectionId;
        Title = source.Title;
        Order = source.Order;
        TitleTranslations = source.TitleTranslations;
        return changed;
    }
}

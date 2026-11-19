namespace Prepround.Domain.Catalog;

/// <summary>A direction of a field, such as Backend.</summary>
/// <remarks>Order is the 1-based position in the field file.</remarks>
public sealed class Direction : CatalogEntity
{
    public Direction(string fieldId, string id, string title, int order)
    {
        FieldId = fieldId;
        Id = id;
        Title = title;
        Order = order;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string Title { get; private set; }

    public int Order { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the direction.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Direction source)
    {
        var changed = Activate() | Title != source.Title | Order != source.Order;
        Title = source.Title;
        Order = source.Order;
        return changed;
    }
}

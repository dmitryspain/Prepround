namespace Prepround.Domain.Catalog;

/// <summary>A field such as IT: owns its level scale, directions and topics.</summary>
public sealed class Field : CatalogEntity
{
    public Field(string id, string title)
    {
        Id = id;
        Title = title;
    }

    public string Id { get; private set; }

    public string Title { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the field.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Field source)
    {
        var changed = Activate() | Title != source.Title;
        Title = source.Title;
        return changed;
    }
}

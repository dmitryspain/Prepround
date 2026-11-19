namespace Prepround.Domain.Catalog;

/// <summary>A topic of the field's catalog, shared by every stack that lists it.</summary>
public sealed class Topic : CatalogEntity
{
    public Topic(string fieldId, string id, string title, string description)
    {
        FieldId = fieldId;
        Id = id;
        Title = title;
        Description = description;
    }

    public string FieldId { get; private set; }

    public string Id { get; private set; }

    public string Title { get; private set; }

    /// <summary>Optional in the files; empty when absent.</summary>
    public string Description { get; private set; }

    /// <summary>Takes the values of <paramref name="source"/>, built from the files, and reactivates the topic.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(Topic source)
    {
        var changed = Activate() | Title != source.Title | Description != source.Description;
        Title = source.Title;
        Description = source.Description;
        return changed;
    }
}

namespace Prepround.Domain.Catalog;

/// <summary>A topic in a stack's default set; deactivated, not deleted, when the stack no longer lists it.</summary>
/// <remarks>Order is the 1-based position in the stack's topic list.</remarks>
public sealed class StackTopic : CatalogEntity
{
    public StackTopic(string fieldId, string stackId, string topicId, int order)
    {
        FieldId = fieldId;
        StackId = stackId;
        TopicId = topicId;
        Order = order;
    }

    public string FieldId { get; private set; }

    public string StackId { get; private set; }

    public string TopicId { get; private set; }

    public int Order { get; private set; }

    /// <summary>Takes the position of <paramref name="source"/>, built from the files, and reactivates the link.</summary>
    /// <returns>True when the row changed.</returns>
    public bool UpdateFrom(StackTopic source)
    {
        var changed = Activate() | Order != source.Order;
        Order = source.Order;
        return changed;
    }
}

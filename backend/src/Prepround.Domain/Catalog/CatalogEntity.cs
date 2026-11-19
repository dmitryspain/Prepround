namespace Prepround.Domain.Catalog;

/// <summary>
/// Base of every catalog row imported from content. Rows are never deleted, because clients keep ids in their
/// history: an entity that is no longer in the files is only deactivated, and reactivated when it returns (ADR 002).
/// </summary>
public abstract class CatalogEntity
{
    public bool IsActive { get; private set; } = true;

    /// <returns>True when the entity was active, so the row changes.</returns>
    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }

    /// <summary>Back in the files. Combine with a non-short-circuit <c>|</c>, so it runs even after a change is found.</summary>
    /// <returns>True when the entity was inactive, so the row changes.</returns>
    protected bool Activate()
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        return true;
    }
}

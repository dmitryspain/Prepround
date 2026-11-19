namespace Prepround.Application.Catalog;

/// <summary>Read side of the catalog. The implementation lives in Infrastructure, so Application stays free of EF Core.</summary>
public interface ICatalogReadRepository
{
    /// <summary>Every active row, untracked; questions come only as counts, so their text is never loaded.</summary>
    Task<ActiveCatalog> GetActiveAsync(CancellationToken cancellationToken);
}

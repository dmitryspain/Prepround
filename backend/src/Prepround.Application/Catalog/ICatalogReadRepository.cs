namespace Prepround.Application.Catalog;

/// <summary>Read side of the catalog. The implementation lives in Infrastructure, so Application stays free of EF Core.</summary>
public interface ICatalogReadRepository
{
    /// <summary>Every active row, untracked; questions come only as counts, so their text is never loaded.</summary>
    /// <param name="language">
    /// A language of <see cref="Content.ContentLanguages.All"/>. Counts take only the questions available in it:
    /// every one in the base language, only the translated ones in a translation language (ADR 006, section 3).
    /// </param>
    Task<ActiveCatalog> GetActiveAsync(string language, CancellationToken cancellationToken);
}

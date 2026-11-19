namespace Prepround.Application.Content.Import;

/// <summary>Persistence of an import. The implementation lives in Infrastructure, so Application stays free of EF Core.</summary>
public interface ICatalogImportRepository
{
    /// <summary>Checks the database before anything is read from it; never throws when it is unreachable.</summary>
    Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>Every catalog row, active or not. Changes made to these entities are written by <see cref="SaveAsync"/>.</summary>
    Task<CatalogRows> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Inserts <paramref name="newRows"/> and writes the changes made to the loaded rows, all in one transaction.</summary>
    Task SaveAsync(CatalogRows newRows, CancellationToken cancellationToken);
}

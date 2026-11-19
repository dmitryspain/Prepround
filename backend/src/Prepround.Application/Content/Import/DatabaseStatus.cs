namespace Prepround.Application.Content.Import;

/// <param name="IsReachable">Whether a connection to the database could be opened.</param>
/// <param name="PendingMigrations">Schema migrations not yet applied; empty when the schema is current or unknown.</param>
public sealed record DatabaseStatus(bool IsReachable, IReadOnlyList<string> PendingMigrations);

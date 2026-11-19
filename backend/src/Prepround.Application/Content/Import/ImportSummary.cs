namespace Prepround.Application.Content.Import;

/// <summary>What an import changed, per entity type. A repeated import of the same files gives all zeros.</summary>
/// <remarks>A change in a stack's topic list or its order counts as an update of the stack.</remarks>
public sealed record ImportSummary(
    ImportCounts Fields,
    ImportCounts Levels,
    ImportCounts Directions,
    ImportCounts Stacks,
    ImportCounts Topics,
    ImportCounts Questions);

/// <param name="Updated">Existing rows whose values changed or that were reactivated.</param>
public sealed record ImportCounts(int Added, int Updated, int Deactivated);

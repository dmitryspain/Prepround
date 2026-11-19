namespace Prepround.Application.Content;

public sealed record DirectionDefinition(string Id, string Title, IReadOnlyList<StackDefinition> Stacks);

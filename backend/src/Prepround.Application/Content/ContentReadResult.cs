namespace Prepround.Application.Content;

/// <summary>
/// What a content reader produced. Errors here are structural (YAML syntax, unknown keys, wrong value shapes);
/// the rules of ADR 002 are checked separately by <see cref="ContentValidator"/>.
/// </summary>
public sealed record ContentReadResult(IReadOnlyList<FieldDefinition> Fields, IReadOnlyList<ContentError> Errors);

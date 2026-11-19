namespace Prepround.Application.Content;

/// <summary>
/// A field read from <c>&lt;field&gt;/field.yaml</c> together with its topic files.
/// Levels are ordered from the lowest to the highest: the position is the rank (ADR 002).
/// </summary>
/// <param name="File">Path of field.yaml relative to the content root, with forward slashes; used in errors and for rule 3.</param>
public sealed record FieldDefinition(
    string File,
    string Id,
    string Title,
    IReadOnlyList<LevelDefinition> Levels,
    IReadOnlyList<DirectionDefinition> Directions,
    IReadOnlyList<TopicDefinition> Topics);

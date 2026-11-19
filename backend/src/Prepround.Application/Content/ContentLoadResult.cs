namespace Prepround.Application.Content;

/// <summary>
/// Content after <see cref="ContentLoader"/>: the fields as read and every problem found, structural or by rules 1–8.
/// The fields are fit for use only when <see cref="Errors"/> is empty.
/// </summary>
public sealed record ContentLoadResult(IReadOnlyList<FieldDefinition> Fields, IReadOnlyList<ContentError> Errors);

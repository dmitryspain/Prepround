namespace Prepround.Application.Content;

/// <param name="File">Path of the topic file relative to the content root, with forward slashes; used in errors and for rule 3.</param>
public sealed record TopicDefinition(
    string File,
    string Id,
    string Title,
    string Description,
    IReadOnlyList<QuestionDefinition> Questions);

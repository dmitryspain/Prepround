namespace Prepround.Application.Content;

/// <param name="File">Path of the topic file relative to the content root, with forward slashes; used in errors and for rule 3.</param>
/// <param name="IsDraft">
/// The topic's questions have not passed the fact check yet (ADR 002, "draft topics"). Validated like any other topic;
/// import leaves its questions out unless drafts are included explicitly. Absent in the file means false.
/// </param>
public sealed record TopicDefinition(
    string File,
    string Id,
    string Title,
    string Description,
    IReadOnlyList<QuestionDefinition> Questions,
    bool IsDraft = false);

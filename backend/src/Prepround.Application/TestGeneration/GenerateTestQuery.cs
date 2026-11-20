using System.Text.Json.Serialization;
using Prepround.Application.Abstractions.Messaging;

namespace Prepround.Application.TestGeneration;

/// <summary>
/// A new test by the user's settings (spec 5.2). Bound straight from the JSON body, so every member is nullable:
/// a missing value reaches the handler and gets its own error code instead of a generic model-binding error.
/// </summary>
/// <param name="ExcludeQuestionIds">Questions already seen on the device; they are used only when fresh ones run out.</param>
public sealed record GenerateTestQuery(
    string? FieldId,
    IReadOnlyList<string?>? LevelIds,
    IReadOnlyList<TestTopicRequest?>? Topics,
    IReadOnlyList<string?>? ExcludeQuestionIds) : IQuery<GeneratedTestResponse>
{
    /// <summary>
    /// <c>?lang=</c> as sent; missing or empty gives the base language (ADR 006). Set by the controller from the query
    /// string and never read from the body, whose shape does not depend on the language (spec 5).
    /// </summary>
    [JsonIgnore]
    public string? Language { get; init; }
}

/// <param name="Count">Questions to take from the topic; a missing value binds as 0 and fails as too small.</param>
public sealed record TestTopicRequest(string? TopicId, int Count);

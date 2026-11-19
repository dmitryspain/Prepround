namespace Prepround.Application.TestGeneration;

/// <summary>Shape of <c>POST /api/tests</c> (spec 5.2); the API serializes it in camelCase.</summary>
/// <param name="Questions">The whole test, already shuffled across topics.</param>
public sealed record GeneratedTestResponse(IReadOnlyList<TestQuestionResponse> Questions);

/// <param name="Type">The same words as in the content files: single or multiple.</param>
/// <param name="Code">Null when the question has no code block.</param>
/// <param name="Options">Shuffled; the client checks answers itself, so correctness travels with them (ADR 001).</param>
public sealed record TestQuestionResponse(
    string Id,
    string TopicId,
    string LevelId,
    string Type,
    string Text,
    QuestionCodeResponse? Code,
    IReadOnlyList<TestOptionResponse> Options,
    string Explanation);

public sealed record QuestionCodeResponse(string Lang, string Source);

/// <param name="Key">0-based index of the option in the file: it survives the shuffle, so answers map back to options.</param>
public sealed record TestOptionResponse(int Key, string Text, bool IsCorrect);

namespace Prepround.Domain.Catalog;

/// <summary>An answer option. Its index in <see cref="Question.Options"/> is its stable key (spec 5.2).</summary>
public sealed record QuestionOption(string Text, bool IsCorrect);

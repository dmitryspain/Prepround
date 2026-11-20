using Prepround.Domain.Catalog;

namespace Prepround.Application.TestGeneration;

/// <summary>Read side of test generation. The implementation lives in Infrastructure, so Application stays free of EF Core.</summary>
public interface ITestGenerationRepository
{
    /// <summary>The field with its active levels and topics, untracked; null when no active field has this id.</summary>
    Task<ActiveField?> GetActiveFieldAsync(string fieldId, CancellationToken cancellationToken);

    /// <summary>
    /// Every question of the topics available in <paramref name="language"/>, active or not, on every level. Only ids,
    /// levels and the active flag are loaded: the handler filters them, and the text of questions that are not picked
    /// is never read.
    /// </summary>
    /// <param name="language">
    /// A language of <see cref="Content.ContentLanguages.All"/>: the base language gives every question, a translation
    /// language only the questions translated to it (ADR 006, section 3).
    /// </param>
    Task<IReadOnlyList<QuestionCandidate>> GetCandidatesAsync(
        string fieldId,
        IReadOnlyCollection<string> topicIds,
        string language,
        CancellationToken cancellationToken);

    /// <summary>Full data of the picked questions, untracked, in no particular order.</summary>
    Task<IReadOnlyList<Question>> GetQuestionsAsync(IReadOnlyCollection<string> questionIds, CancellationToken cancellationToken);

    /// <summary>
    /// Translations of the picked questions to a translation language, untracked, in no particular order; a question
    /// without one is absent. Read in one go for the picked questions only, never for the whole topic.
    /// </summary>
    Task<IReadOnlyList<QuestionTranslation>> GetTranslationsAsync(
        IReadOnlyCollection<string> questionIds,
        string language,
        CancellationToken cancellationToken);
}

/// <param name="Levels">Active levels of the field only.</param>
/// <param name="Topics">Active topics of the field only.</param>
public sealed record ActiveField(Field Field, IReadOnlyList<Level> Levels, IReadOnlyList<Topic> Topics);

public sealed record QuestionCandidate(string Id, string TopicId, string LevelId, bool IsActive);

using Microsoft.EntityFrameworkCore;
using Prepround.Application.TestGeneration;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Repositories;

public sealed class TestGenerationRepository : ITestGenerationRepository
{
    private readonly PreproundContext _context;

    public TestGenerationRepository(PreproundContext context)
    {
        _context = context;
    }

    /// <summary>Untracked: nothing here is written back.</summary>
    public async Task<ActiveField?> GetActiveFieldAsync(string fieldId, CancellationToken cancellationToken)
    {
        var field = await _context.Fields.AsNoTracking()
            .SingleOrDefaultAsync(field => field.Id == fieldId && field.IsActive, cancellationToken);
        if (field is null)
        {
            return null;
        }

        var levels = await _context.Levels.AsNoTracking()
            .Where(level => level.FieldId == fieldId && level.IsActive)
            .ToListAsync(cancellationToken);
        var topics = await _context.Topics.AsNoTracking()
            .Where(topic => topic.FieldId == fieldId && topic.IsActive)
            .ToListAsync(cancellationToken);

        return new ActiveField(field, levels, topics);
    }

    /// <summary>A narrow projection: text, options and explanations of questions that are not picked stay in the database.</summary>
    public async Task<IReadOnlyList<QuestionCandidate>> GetCandidatesAsync(
        string fieldId,
        IReadOnlyCollection<string> topicIds,
        string language,
        CancellationToken cancellationToken)
    {
        var ids = topicIds.ToArray();

        return await _context.Questions.AsNoTracking()
            .Where(question => question.FieldId == fieldId && ids.Contains(question.TopicId))
            .AvailableIn(_context, language)
            .Select(question => new QuestionCandidate(question.Id, question.TopicId, question.LevelId, question.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Question>> GetQuestionsAsync(
        IReadOnlyCollection<string> questionIds,
        CancellationToken cancellationToken)
    {
        var ids = questionIds.ToArray();

        return await _context.Questions.AsNoTracking()
            .Where(question => ids.Contains(question.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>One query for all the picked questions, so a test of 100 questions costs no more round trips than one of 10.</summary>
    public async Task<IReadOnlyList<QuestionTranslation>> GetTranslationsAsync(
        IReadOnlyCollection<string> questionIds,
        string language,
        CancellationToken cancellationToken)
    {
        var ids = questionIds.ToArray();

        return await _context.QuestionTranslations.AsNoTracking()
            .Where(translation => translation.Language == language && ids.Contains(translation.QuestionId))
            .ToListAsync(cancellationToken);
    }
}

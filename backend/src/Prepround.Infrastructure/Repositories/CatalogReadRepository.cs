using Microsoft.EntityFrameworkCore;
using Prepround.Application.Catalog;

namespace Prepround.Infrastructure.Repositories;

public sealed class CatalogReadRepository : ICatalogReadRepository
{
    private readonly PreproundContext _context;

    public CatalogReadRepository(PreproundContext context)
    {
        _context = context;
    }

    /// <summary>Untracked: nothing here is written back. Ordering is left to the handler, away from database collations.</summary>
    public async Task<ActiveCatalog> GetActiveAsync(CancellationToken cancellationToken) => new(
        await _context.Fields.AsNoTracking().Where(field => field.IsActive).ToListAsync(cancellationToken),
        await _context.Levels.AsNoTracking().Where(level => level.IsActive).ToListAsync(cancellationToken),
        await _context.Directions.AsNoTracking().Where(direction => direction.IsActive).ToListAsync(cancellationToken),
        await _context.Stacks.AsNoTracking().Where(stack => stack.IsActive).ToListAsync(cancellationToken),
        await _context.StackTopics.AsNoTracking().Where(link => link.IsActive).ToListAsync(cancellationToken),
        await _context.Topics.AsNoTracking().Where(topic => topic.IsActive).ToListAsync(cancellationToken),
        // Counted by the database: the catalog needs no question text or options.
        await _context.Questions.AsNoTracking()
            .Where(question => question.IsActive)
            .GroupBy(question => new { question.FieldId, question.TopicId, question.LevelId })
            .Select(group => new TopicLevelCount(group.Key.FieldId, group.Key.TopicId, group.Key.LevelId, group.Count()))
            .ToListAsync(cancellationToken));
}

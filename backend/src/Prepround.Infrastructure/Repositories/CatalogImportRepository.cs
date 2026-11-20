using Microsoft.EntityFrameworkCore;
using Prepround.Application.Content.Import;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Repositories;

public sealed class CatalogImportRepository : ICatalogImportRepository
{
    private readonly PreproundContext _context;

    public CatalogImportRepository(PreproundContext context)
    {
        _context = context;
    }

    public async Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        // CanConnectAsync swallows connection errors and answers false, which is exactly the question asked here.
        if (!await _context.Database.CanConnectAsync(cancellationToken))
        {
            return new DatabaseStatus(false, []);
        }

        var pending = await _context.Database.GetPendingMigrationsAsync(cancellationToken);
        return new DatabaseStatus(true, pending.ToList());
    }

    /// <summary>Tracked on purpose: the handler changes these entities and <see cref="SaveAsync"/> writes the changes.</summary>
    public async Task<CatalogRows> LoadAsync(CancellationToken cancellationToken) => new(
        await _context.Fields.ToListAsync(cancellationToken),
        await _context.Levels.ToListAsync(cancellationToken),
        await _context.Directions.ToListAsync(cancellationToken),
        await _context.Stacks.ToListAsync(cancellationToken),
        await _context.StackTopics.ToListAsync(cancellationToken),
        await _context.Topics.ToListAsync(cancellationToken),
        await _context.Questions.ToListAsync(cancellationToken),
        await _context.QuestionTranslations.ToListAsync(cancellationToken));

    public async Task SaveAsync(
        CatalogRows newRows, IReadOnlyList<QuestionTranslation> removedTranslations, CancellationToken cancellationToken)
    {
        _context.Fields.AddRange(newRows.Fields);
        _context.Levels.AddRange(newRows.Levels);
        _context.Directions.AddRange(newRows.Directions);
        _context.Stacks.AddRange(newRows.Stacks);
        _context.StackTopics.AddRange(newRows.StackTopics);
        _context.Topics.AddRange(newRows.Topics);
        _context.Questions.AddRange(newRows.Questions);
        // Inserted after their questions: EF orders the commands by the foreign key.
        _context.QuestionTranslations.AddRange(newRows.QuestionTranslations);
        _context.QuestionTranslations.RemoveRange(removedTranslations);

        // One transaction for the whole import (ADR 002), stated here instead of left to SaveChanges' defaults:
        // either every row is written or none is.
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}

using Prepround.Application.Abstractions.Messaging;
using Prepround.Domain.Catalog;
using Prepround.Domain.Shared;

namespace Prepround.Application.Content.Import;

/// <summary>
/// Puts the content files into the database (ADR 002): the same read-and-check step as validate, then an upsert by id.
/// Nothing is deleted: rows missing from the files are deactivated, and reactivated when they return.
/// Questions of draft topics count as missing unless <see cref="ImportContentCommand.IncludeDrafts"/> is set.
/// </summary>
public sealed class ImportContentHandler : ICommandHandler<ImportContentCommand, ImportSummary>
{
    private readonly ContentLoader _loader;
    private readonly ICatalogImportRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ImportContentHandler(ContentLoader loader, ICatalogImportRepository repository, TimeProvider timeProvider)
    {
        _loader = loader;
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ImportSummary>> Handle(ImportContentCommand command, CancellationToken cancellationToken)
    {
        // Reading stays synchronous: the files are local, and the database is not touched until all of them pass.
        var content = _loader.Load(command.RootDirectory);
        if (content.Errors.Count > 0)
        {
            return Result.Failure<ImportSummary>(ImportContentErrors.ContentInvalid(content.Errors));
        }

        var status = await _repository.GetStatusAsync(cancellationToken);
        if (!status.IsReachable)
        {
            return Result.Failure<ImportSummary>(ImportContentErrors.DatabaseUnavailable);
        }

        // Writing the current model into an older schema would fail halfway through with a raw SQL error.
        if (status.PendingMigrations.Count > 0)
        {
            return Result.Failure<ImportSummary>(ImportContentErrors.MigrationsPending(status.PendingMigrations));
        }

        var rows = await _repository.LoadAsync(cancellationToken);
        var files = FromFiles(content.Fields, command.IncludeDrafts, _timeProvider.GetUtcNow());
        var newRows = new NewRows();

        // Stack topics go first: a change in a stack's topic list or order counts as an update of the stack itself.
        var changedStacks = new HashSet<(string FieldId, string StackId)>();
        Sync(rows.StackTopics, files.StackTopics, link => (link.FieldId, link.StackId, link.TopicId),
            (row, source) => row.UpdateFrom(source), newRows.StackTopics,
            changed: link => changedStacks.Add((link.FieldId, link.StackId)));

        var summary = new ImportSummary(
            Fields: Sync(rows.Fields, files.Fields, field => field.Id,
                (row, source) => row.UpdateFrom(source), newRows.Fields),
            Levels: Sync(rows.Levels, files.Levels, level => (level.FieldId, level.Id),
                (row, source) => row.UpdateFrom(source), newRows.Levels),
            Directions: Sync(rows.Directions, files.Directions, direction => (direction.FieldId, direction.Id),
                (row, source) => row.UpdateFrom(source), newRows.Directions),
            Stacks: Sync(rows.Stacks, files.Stacks, stack => (stack.FieldId, stack.Id),
                (row, source) => row.UpdateFrom(source) | changedStacks.Contains((row.FieldId, row.Id)), newRows.Stacks),
            Topics: Sync(rows.Topics, files.Topics, topic => (topic.FieldId, topic.Id),
                (row, source) => row.UpdateFrom(source), newRows.Topics),
            Questions: Sync(rows.Questions, files.Questions, question => question.Id,
                (row, source) => row.UpdateFrom(source), newRows.Questions));

        await _repository.SaveAsync(newRows.ToCatalogRows(), cancellationToken);

        return summary;
    }

    /// <summary>
    /// Upsert by key: a row from the files is added to <paramref name="newRows"/> when new and updated when it exists;
    /// every existing row missing from the files is deactivated.
    /// </summary>
    /// <param name="changed">Called for every added, updated or deactivated row.</param>
    private static ImportCounts Sync<TEntity, TKey>(
        IReadOnlyList<TEntity> rows,
        IReadOnlyList<TEntity> sources,
        Func<TEntity, TKey> keyOf,
        Func<TEntity, TEntity, bool> update,
        List<TEntity> newRows,
        Action<TEntity>? changed = null)
        where TEntity : CatalogEntity
        where TKey : notnull
    {
        var existing = rows.ToDictionary(keyOf);
        var inFiles = new HashSet<TKey>();
        int addedCount = 0, updatedCount = 0, deactivatedCount = 0;

        foreach (var source in sources)
        {
            var key = keyOf(source);
            inFiles.Add(key);

            if (!existing.TryGetValue(key, out var row))
            {
                newRows.Add(source);
                changed?.Invoke(source);
                addedCount++;
            }
            else if (update(row, source))
            {
                changed?.Invoke(row);
                updatedCount++;
            }
        }

        foreach (var (key, row) in existing)
        {
            if (!inFiles.Contains(key) && row.Deactivate())
            {
                changed?.Invoke(row);
                deactivatedCount++;
            }
        }

        return new ImportCounts(addedCount, updatedCount, deactivatedCount);
    }

    /// <summary>The catalog as the files describe it. Rank and orders are 1-based positions in the files.</summary>
    /// <param name="includeDrafts">When false, the questions of draft topics are left out as if absent from the files.</param>
    private static CatalogRows FromFiles(IReadOnlyList<FieldDefinition> definitions, bool includeDrafts, DateTimeOffset now)
    {
        var fields = new List<Field>();
        var levels = new List<Level>();
        var directions = new List<Direction>();
        var stacks = new List<Stack>();
        var stackTopics = new List<StackTopic>();
        var topics = new List<Topic>();
        var questions = new List<Question>();

        foreach (var field in definitions)
        {
            fields.Add(new Field(field.Id, field.Title));

            for (var i = 0; i < field.Levels.Count; i++)
            {
                levels.Add(new Level(field.Id, field.Levels[i].Id, field.Levels[i].Title, i + 1));
            }

            for (var i = 0; i < field.Directions.Count; i++)
            {
                var direction = field.Directions[i];
                directions.Add(new Direction(field.Id, direction.Id, direction.Title, i + 1));

                for (var j = 0; j < direction.Stacks.Count; j++)
                {
                    var stack = direction.Stacks[j];
                    stacks.Add(new Stack(field.Id, stack.Id, direction.Id, stack.Title, j + 1));

                    for (var k = 0; k < stack.TopicIds.Count; k++)
                    {
                        stackTopics.Add(new StackTopic(field.Id, stack.Id, stack.TopicIds[k], k + 1));
                    }
                }
            }

            foreach (var topic in field.Topics)
            {
                // A draft topic itself stays in the catalog; only its questions count as absent (ADR 002), so the sync
                // below adds none of them and deactivates those imported before: also the emergency switch-off.
                topics.Add(new Topic(field.Id, topic.Id, topic.Title, topic.Description));
                if (includeDrafts || !topic.IsDraft)
                {
                    questions.AddRange(topic.Questions.Select(question => ToQuestion(field.Id, topic.Id, question, now)));
                }
            }
        }

        return new CatalogRows(fields, levels, directions, stacks, stackTopics, topics, questions);
    }

    private static Question ToQuestion(string fieldId, string topicId, QuestionDefinition question, DateTimeOffset now) =>
        new(
            question.Id,
            fieldId,
            topicId,
            question.LevelId,
            // The validator has already accepted only "single" or "multiple".
            Enum.Parse<QuestionType>(question.Type, ignoreCase: true),
            question.Text,
            question.Code?.Lang,
            question.Code?.Source,
            question.Options.Select(option => new QuestionOption(option.Text, option.IsCorrect)).ToList(),
            question.Explanation,
            now);

    private sealed class NewRows
    {
        public List<Field> Fields { get; } = [];

        public List<Level> Levels { get; } = [];

        public List<Direction> Directions { get; } = [];

        public List<Stack> Stacks { get; } = [];

        public List<StackTopic> StackTopics { get; } = [];

        public List<Topic> Topics { get; } = [];

        public List<Question> Questions { get; } = [];

        public CatalogRows ToCatalogRows() => new(Fields, Levels, Directions, Stacks, StackTopics, Topics, Questions);
    }
}

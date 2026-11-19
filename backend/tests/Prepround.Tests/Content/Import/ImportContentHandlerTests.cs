using Prepround.Application.Content;
using Prepround.Application.Content.Import;
using Prepround.Infrastructure.Content;

namespace Prepround.Tests.Content.Import;

/// <summary>
/// ImportContentHandler with hand-written fakes: content problems and database problems stop the import
/// before anything is written, and UpdatedAt follows the injected clock only when the content changed.
/// </summary>
public class ImportContentHandlerTests
{
    private static readonly ContentError ReaderError =
        new("it/topics/sql.yml", string.Empty, "Файл не буде прочитано.");

    private static readonly DateTimeOffset FirstImport = new(2026, 11, 14, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SecondImport = FirstImport.AddHours(1);

    [Fact]
    public async Task Reader_error_fails_with_content_invalid_and_never_touches_the_repository()
    {
        var repository = new FakeCatalogImportRepository();
        var handler = Handler(new FakeContentReader(new ContentReadResult([], [ReaderError])), repository);

        var result = await handler.Handle(new ImportContentCommand("content-root"), TestContext.Current.CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Equal("Content.Invalid", result.Error.Code);
        Assert.Equal(ReaderError.ToString(), result.Error.Message);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Rule_errors_are_joined_one_per_line_and_never_touch_the_repository()
    {
        using var content = new ContentFixture();
        content.Replace(ContentFixture.AlphaFile, "type: single", "type: singel");
        content.Replace(ContentFixture.FieldFile, "[alpha, beta]", "[alpha, beta, gamma]");
        var expected = new ContentLoader(new YamlContentReader(), new ContentValidator()).Load(content.Root).Errors;
        var repository = new FakeCatalogImportRepository();

        var result = await Handler(new YamlContentReader(), repository)
            .Handle(new ImportContentCommand(content.Root), TestContext.Current.CancellationToken);

        Assert.Equal(2, expected.Count);
        Assert.Equal("Content.Invalid", result.Error.Code);
        Assert.Equal(expected.Select(error => error.ToString()), result.Error.Message.Split('\n'));
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task Unreachable_database_fails_before_loading_or_saving()
    {
        using var content = new ContentFixture();
        var repository = new FakeCatalogImportRepository { Status = new DatabaseStatus(false, []) };

        var result = await Handler(new YamlContentReader(), repository)
            .Handle(new ImportContentCommand(content.Root), TestContext.Current.CancellationToken);

        Assert.Equal(ImportContentErrors.DatabaseUnavailableCode, result.Error.Code);
        Assert.Equal((1, 0, 0), (repository.StatusCalls, repository.LoadCalls, repository.SaveCalls));
    }

    [Fact]
    public async Task Pending_migrations_fail_before_loading_or_saving_and_are_named()
    {
        using var content = new ContentFixture();
        var repository = new FakeCatalogImportRepository { Status = new DatabaseStatus(true, ["20261114000242_InitialCatalog"]) };

        var result = await Handler(new YamlContentReader(), repository)
            .Handle(new ImportContentCommand(content.Root), TestContext.Current.CancellationToken);

        Assert.Equal(ImportContentErrors.MigrationsPendingCode, result.Error.Code);
        Assert.Contains("20261114000242_InitialCatalog", result.Error.Message);
        Assert.Equal((1, 0, 0), (repository.StatusCalls, repository.LoadCalls, repository.SaveCalls));
    }

    [Fact]
    public async Task UpdatedAt_takes_the_clock_time_only_for_the_question_whose_content_changed()
    {
        using var content = new ContentFixture();
        var repository = new FakeCatalogImportRepository();
        var clock = new FakeTimeProvider(FirstImport);
        var ct = TestContext.Current.CancellationToken;
        var first = await Handler(new YamlContentReader(), repository, clock).Handle(new ImportContentCommand(content.Root), ct);
        Assert.Equal(new ImportCounts(2, 0, 0), first.Value.Questions);

        clock.Now = SecondImport;
        content.Replace(ContentFixture.AlphaFile, "explanation: Бо так.", "explanation: Бо так, і ось чому.");
        var second = await Handler(new YamlContentReader(), repository, clock).Handle(new ImportContentCommand(content.Root), ct);

        Assert.Equal(new ImportCounts(0, 1, 0), second.Value.Questions);
        Assert.Equal(new ImportCounts(0, 0, 0), second.Value.Topics);
        var questions = repository.Stored.Questions.ToDictionary(question => question.Id);
        Assert.Equal(SecondImport, questions["alpha-001"].UpdatedAt);
        Assert.Equal("Бо так, і ось чому.", questions["alpha-001"].Explanation);
        Assert.Equal(FirstImport, questions["alpha-002"].UpdatedAt);
    }

    private static ImportContentHandler Handler(
        IContentReader reader, ICatalogImportRepository repository, TimeProvider? clock = null) =>
        new(new ContentLoader(reader, new ContentValidator()), repository, clock ?? new FakeTimeProvider(FirstImport));
}

/// <summary>
/// Keeps rows in memory like a tracking context: LoadAsync hands out the stored instances, so the handler's changes
/// to them are "written", and SaveAsync appends the new rows. Counts every call.
/// </summary>
internal sealed class FakeCatalogImportRepository : ICatalogImportRepository
{
    public DatabaseStatus Status { get; init; } = new(true, []);

    public CatalogRows Stored { get; private set; } = new([], [], [], [], [], [], []);

    public int StatusCalls { get; private set; }

    public int LoadCalls { get; private set; }

    public int SaveCalls { get; private set; }

    public int Calls => StatusCalls + LoadCalls + SaveCalls;

    public Task<DatabaseStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        StatusCalls++;
        return Task.FromResult(Status);
    }

    public Task<CatalogRows> LoadAsync(CancellationToken cancellationToken)
    {
        LoadCalls++;
        return Task.FromResult(Stored);
    }

    public Task SaveAsync(CatalogRows newRows, CancellationToken cancellationToken)
    {
        SaveCalls++;
        Stored = new CatalogRows(
            [.. Stored.Fields, .. newRows.Fields],
            [.. Stored.Levels, .. newRows.Levels],
            [.. Stored.Directions, .. newRows.Directions],
            [.. Stored.Stacks, .. newRows.Stacks],
            [.. Stored.StackTopics, .. newRows.StackTopics],
            [.. Stored.Topics, .. newRows.Topics],
            [.. Stored.Questions, .. newRows.Questions]);
        return Task.CompletedTask;
    }
}

internal sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prepround.Application;
using Prepround.Application.Abstractions.Messaging;
using Prepround.Application.Content;
using Prepround.Application.Content.Import;
using Prepround.ContentTool;
using Prepround.Domain.Shared;
using Prepround.Infrastructure;

// Exit codes let scripts tell content errors (1) apart from usage errors (2) and database problems (3).
const int SuccessExitCode = 0;
const int ContentErrorExitCode = 1;
const int UsageErrorExitCode = 2;
const int DatabaseErrorExitCode = 3;

const string ConnectionStringName = Prepround.Infrastructure.DependencyInjection.ConnectionStringName;
const string IncludeDraftsFlag = "--include-drafts";

const string Usage = $"""
    Prepround.ContentTool — перевірка та імпорт контенту з YAML.

    Використання:
      Prepround.ContentTool <команда> <тека> [прапорець]

    Команди:
      validate <тека>                    перевірити контент без бази даних
      import <тека> [{IncludeDraftsFlag}]   перевірити контент та імпортувати його в базу даних

    Прапорець import (лише після теки):
      {IncludeDraftsFlag}   імпортувати й питання тем-чернеток (draft: true). Без нього вони
                         не імпортуються, а раніше імпортовані деактивуються.
                         Так само з перекладами питань у темі з draft_<мова>: true: без
                         прапорця вони не імпортуються, а раніше імпортовані видаляються.
                         Dev-база імпортує з прапорцем, прод — без нього.
    """;

// The command from README.md, without the password: the full one is there.
const string MissingConnectionString = $"""
    Не задано рядок підключення до бази даних (ConnectionStrings:{ConnectionStringName}).
    Задайте його один раз у user-secrets з теки backend/ (повна команда з паролем локального контейнера — у README.md):
      dotnet user-secrets set "ConnectionStrings:{ConnectionStringName}" "Host=localhost;Port=5433;Database=prepround;Username=prepround;Password=<пароль>" --project src/Prepround.Api
    Або передайте його в змінній середовища ConnectionStrings__{ConnectionStringName}.
    """;

const string ApplyMigrationsHint = """
    Застосуйте міграції з теки backend/:
      dotnet ef database update --project src/Prepround.Infrastructure --startup-project src/Prepround.Api
    """;

switch (args)
{
    case []:
        return UsageError(null);
    case ["validate", var directory]:
        return Validate(directory);
    case ["validate", ..]:
        return UsageError("Команді validate потрібна рівно одна тека з контентом.");
    case ["import", IncludeDraftsFlag, ..]:
        return UsageError($"Прапорець {IncludeDraftsFlag} пишеться після теки: import <тека> {IncludeDraftsFlag}.");
    case ["import", var directory]:
        return await Import(directory, includeDrafts: false);
    case ["import", var directory, IncludeDraftsFlag]:
        return await Import(directory, includeDrafts: true);
    case ["import", _, var flag] when flag.StartsWith('-'):
        return UsageError($"Невідомий прапорець команди import: {flag}. Допустимий лише {IncludeDraftsFlag}.");
    case ["import", ..]:
        return UsageError("Команді import потрібна рівно одна тека з контентом.");
    default:
        return UsageError($"Невідома команда: {args[0]}");
}

static int Validate(string directory)
{
    if (!ContentDirectoryExists(directory))
    {
        return UsageErrorExitCode;
    }

    using var services = BuildServices(ContentToolConfiguration.Build());
    using var scope = services.CreateScope();
    var content = scope.ServiceProvider.GetRequiredService<ContentLoader>().Load(directory);

    if (content.Errors.Count > 0)
    {
        ReportContentErrors(content.Errors.Select(error => error.ToString()).ToList());
        return ContentErrorExitCode;
    }

    foreach (var field in content.Fields)
    {
        var stackCount = field.Directions.Sum(direction => direction.Stacks.Count);
        var questionCount = field.Topics.Sum(topic => topic.Questions.Count);
        var draftQuestionCount = field.Topics.Where(topic => topic.IsDraft).Sum(topic => topic.Questions.Count);

        // Shown only when there are drafts: that is how many questions a default import leaves out.
        var drafts = draftQuestionCount > 0 ? $", з них у чернетках {draftQuestionCount}" : string.Empty;

        // Likewise only for languages that have fully translated questions, so untranslated content prints as before.
        var translations = string.Concat(ContentLanguages.Translations
            .Select(language => (Language: language, Count: field.Topics
                .Sum(topic => topic.Questions.Count(question => question.IsTranslatedTo(language)))))
            .Where(translated => translated.Count > 0)
            .Select(translated => $", перекладів {translated.Language}: {translated.Count}"));

        Console.WriteLine(
            $"{field.Id}: рівнів {field.Levels.Count}, напрямів {field.Directions.Count}, " +
            $"стеків {stackCount}, тем {field.Topics.Count}, питань {questionCount}{drafts}{translations}");
    }

    return SuccessExitCode;
}

static async Task<int> Import(string directory, bool includeDrafts)
{
    if (!ContentDirectoryExists(directory))
    {
        return UsageErrorExitCode;
    }

    var configuration = ContentToolConfiguration.Build();
    if (string.IsNullOrWhiteSpace(configuration.GetConnectionString(ConnectionStringName)))
    {
        Console.Error.WriteLine(MissingConnectionString);
        return DatabaseErrorExitCode;
    }

    await using var services = BuildServices(configuration);
    await using var scope = services.CreateAsyncScope();
    var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<ImportContentCommand, ImportSummary>>();

    Result<ImportSummary> result;
    try
    {
        // Ctrl+C is left to its default: the process ends, and Postgres rolls back the uncommitted transaction.
        result = await handler.Handle(new ImportContentCommand(directory, includeDrafts), CancellationToken.None);
    }
    catch (Exception exception) when (IsDatabaseFailure(exception))
    {
        Console.Error.WriteLine($"Помилка бази даних під час імпорту: {exception.GetBaseException().Message}");
        return DatabaseErrorExitCode;
    }

    if (result.IsFailure)
    {
        var error = result.Error;
        if (error.Code == ImportContentErrors.ContentInvalidCode)
        {
            ReportContentErrors(error.Message.Split('\n'));
            return ContentErrorExitCode;
        }

        // Every other failure of the import is about the database.
        Console.Error.WriteLine(error.Message);
        if (error.Code == ImportContentErrors.MigrationsPendingCode)
        {
            Console.Error.WriteLine(ApplyMigrationsHint);
        }

        return DatabaseErrorExitCode;
    }

    var summary = result.Value;
    Console.WriteLine("Імпорт завершено.");
    PrintCounts("Галузі", summary.Fields);
    PrintCounts("Рівні", summary.Levels);
    PrintCounts("Напрями", summary.Directions);
    PrintCounts("Стеки", summary.Stacks);
    PrintCounts("Теми", summary.Topics);
    PrintCounts("Питання", summary.Questions);
    // Right after the questions: a translation wave proves the base content unchanged by these two lines (ADR 006).
    Console.WriteLine(
        $"Переклади питань: додано {summary.QuestionTranslations.Added}, " +
        $"оновлено {summary.QuestionTranslations.Updated}, видалено {summary.QuestionTranslations.Removed}");

    return SuccessExitCode;
}

static bool ContentDirectoryExists(string directory)
{
    if (Directory.Exists(directory))
    {
        return true;
    }

    Console.Error.WriteLine($"Теку контенту не знайдено: {directory}");
    return false;
}

// validate and import print content errors the same way, so the author sees the same lines from both.
static void ReportContentErrors(IReadOnlyCollection<string> lines)
{
    foreach (var line in lines)
    {
        Console.Error.WriteLine(line);
    }

    Console.Error.WriteLine($"Помилок у контенті: {lines.Count}.");
}

static void PrintCounts(string entities, ImportCounts counts) =>
    Console.WriteLine(
        $"{entities}: додано {counts.Added}, оновлено {counts.Updated}, деактивовано {counts.Deactivated}");

// Npgsql errors (connection lost, constraint broken) derive from DbException; EF wraps those of SaveChanges.
static bool IsDatabaseFailure(Exception exception)
{
    for (var current = exception; current is not null; current = current.InnerException)
    {
        if (current is DbException)
        {
            return true;
        }
    }

    return false;
}

// One composition for both commands, so validate checks exactly what import resolves its handler from.
static ServiceProvider BuildServices(IConfiguration configuration)
{
    // validate needs no connection string: the DbContext is configured only when first resolved,
    // and validate never resolves it.
    return new ServiceCollection()
        .AddApplication()
        .AddInfrastructure(configuration)
        .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}

static int UsageError(string? message)
{
    if (message is not null)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine();
    }

    Console.Error.WriteLine(Usage);
    return UsageErrorExitCode;
}

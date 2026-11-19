using Prepround.Domain.Shared;

namespace Prepround.Application.Content.Import;

public static class ImportContentErrors
{
    public const string ContentInvalidCode = "Content.Invalid";
    public const string DatabaseUnavailableCode = "Database.Unavailable";
    public const string MigrationsPendingCode = "Database.MigrationsPending";

    /// <summary>One "file: id: message" line per error, exactly as validate prints them.</summary>
    public static Error ContentInvalid(IEnumerable<ContentError> errors) =>
        new(ContentInvalidCode, string.Join('\n', errors));

    public static readonly Error DatabaseUnavailable = new(DatabaseUnavailableCode,
        "Не вдалося підключитися до бази даних. Перевірте, що PostgreSQL запущено і рядок підключення правильний.");

    public static Error MigrationsPending(IEnumerable<string> migrations) => new(MigrationsPendingCode,
        $"У базі даних не застосовано міграції: {string.Join(", ", migrations)}.");
}

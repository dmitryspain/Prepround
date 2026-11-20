using Prepround.Domain.Shared;

namespace Prepround.Application.Content;

/// <summary>
/// The content language of an API request, <c>?lang=</c> (ADR 006, section 3). One check for every endpoint that
/// takes it, so the catalog and the tests accept and reject exactly the same values.
/// </summary>
public static class RequestLanguage
{
    public const string UnsupportedCode = "Language.Unsupported";

    /// <summary>
    /// Missing or empty gives the base language, so a client that sends none gets what it always got. Any other
    /// value must be one of <see cref="ContentLanguages.All"/> as written: nothing is trimmed or case-folded, so
    /// <c>EN</c> or <c>" en"</c> is rejected rather than guessed.
    /// </summary>
    public static Result<string> Resolve(string? language)
    {
        if (string.IsNullOrEmpty(language))
        {
            return ContentLanguages.Base;
        }

        return ContentLanguages.All.Contains(language, StringComparer.Ordinal)
            ? language
            : Result.Failure<string>(Unsupported(language));
    }

    /// <summary>For the developer, like every API message: the client words the error by its code (ADR 006).</summary>
    public static Error Unsupported(string language) =>
        new(UnsupportedCode, $"Мову «{language}» не підтримано. Доступні: {string.Join(", ", ContentLanguages.All)}.");
}

using Prepround.Application.Content;
using Prepround.Domain.Catalog;

namespace Prepround.Infrastructure.Repositories;

/// <summary>Question filters shared by the catalog counts and the test candidates, so both count the same questions.</summary>
internal static class QuestionQueries
{
    /// <summary>
    /// Questions a client may get in <paramref name="language"/> (ADR 006, section 3): every one in the base language,
    /// only those with a translation to it otherwise. An EXISTS on the translation key, so no translation text is read.
    /// </summary>
    public static IQueryable<Question> AvailableIn(
        this IQueryable<Question> questions,
        PreproundContext context,
        string language) =>
        language == ContentLanguages.Base
            ? questions
            : questions.Where(question => context.QuestionTranslations.Any(
                translation => translation.QuestionId == question.Id && translation.Language == language));
}

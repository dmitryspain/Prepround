using Prepround.Domain.Catalog;
using Prepround.Domain.Shared;

namespace Prepround.Application.TestGeneration;

/// <summary>Validation errors of <c>POST /api/tests</c> (spec 5.2); every one is a 400 with its own code.</summary>
public static class GenerateTestErrors
{
    public const string FieldNotFoundCode = "Test.FieldNotFound";
    public const string LevelsRequiredCode = "Test.LevelsRequired";
    public const string UnknownLevelCode = "Test.UnknownLevel";
    public const string DuplicateLevelCode = "Test.DuplicateLevel";
    public const string TopicsRequiredCode = "Test.TopicsRequired";
    public const string UnknownTopicCode = "Test.UnknownTopic";
    public const string DuplicateTopicCode = "Test.DuplicateTopic";
    public const string CountTooSmallCode = "Test.CountTooSmall";
    public const string TotalOutOfRangeCode = "Test.TotalOutOfRange";
    public const string NotEnoughQuestionsCode = "Test.NotEnoughQuestions";
    public const string TooManyExcludedCode = "Test.TooManyExcluded";

    public static Error FieldNotFound(string? fieldId) =>
        new(FieldNotFoundCode, $"Галузі «{fieldId}» немає в каталозі.");

    public static readonly Error LevelsRequired = new(LevelsRequiredCode, "Виберіть щонайменше один рівень.");

    public static Error UnknownLevel(string? levelId, string fieldId) =>
        new(UnknownLevelCode, $"Рівня «{levelId}» немає в галузі «{fieldId}».");

    public static Error DuplicateLevel(string levelId) =>
        new(DuplicateLevelCode, $"Рівень «{levelId}» вибрано більше одного разу.");

    public static readonly Error TopicsRequired = new(TopicsRequiredCode, "Виберіть щонайменше одну тему.");

    public static Error UnknownTopic(string? topicId, string fieldId) =>
        new(UnknownTopicCode, $"Теми «{topicId}» немає в галузі «{fieldId}».");

    public static Error DuplicateTopic(string topicId) =>
        new(DuplicateTopicCode, $"Тему «{topicId}» вибрано більше одного разу.");

    public static Error CountTooSmall(Topic topic, int count) =>
        new(CountTooSmallCode, $"Для теми «{topic.Title}» ({topic.Id}) потрібно щонайменше одне питання, а запитано {count}.");

    public static Error TotalOutOfRange(long total, int min, int max) =>
        new(TotalOutOfRangeCode, $"Загальна кількість питань має бути від {min} до {max}, а запитано {total}.");

    /// <summary>Names the topic and what is available, so the user knows which count to lower.</summary>
    public static Error NotEnoughQuestions(Topic topic, int requested, int available) =>
        new(NotEnoughQuestionsCode,
            $"У темі «{topic.Title}» ({topic.Id}) на вибраних рівнях недостатньо питань: запитано {requested}, доступно {available}.");

    public static Error TooManyExcluded(int count, int max) =>
        new(TooManyExcludedCode, $"Забагато виключених питань: {count}, а можна не більше {max}.");
}
